import axios from 'axios';
import { defineStore } from 'pinia';
import i18n from '@/i18n';
import { useSettingsStore } from '@/store/settingsStore';

const DEFAULT_BACKEND_PORT = 19091;
const DEFAULT_POLL_INTERVAL_MS = 5000;
const tr = (key, params) => i18n.global.t(`plugins.pinsAllSky.${key}`, params);

export const usePinsAllSkyStore = defineStore('pinsAllSkyStore', {
  state: () => ({
    backendPort: DEFAULT_BACKEND_PORT,
    status: null,
    config: null,
    error: null,
    loading: false,
    saving: false,
    cleanupBusy: false,
    backendUpdateBusy: false,
    manualLabel: '',
    actionMessage: null,
    sessionDetailsById: {},
    detailsLoadingById: {},
    imageNonce: Date.now(),
    pollTimer: null,
  }),

  getters: {
    settingsStore() {
      return useSettingsStore();
    },

    backendBaseUrl() {
      const protocol = this.settingsStore.backendProtocol || 'http';
      const host = this.settingsStore.connection?.ip || window.location.hostname;
      return `${protocol}://${host}:${this.backendPort}`;
    },

    currentImageUrl() {
      if (!this.status?.currentImageUrl) {
        return null;
      }

      return `${this.backendBaseUrl}${this.status.currentImageUrl}?t=${this.imageNonce}`;
    },
  },

  actions: {
    async fetchStatus() {
      const { data } = await axios.get(`${this.backendBaseUrl}/api/status`);
      this.status = data.data;
      this.error = data.success ? null : data.error || tr('errors.loadStatus');
      this.imageNonce = Date.now();
      return this.status;
    },

    async fetchConfig() {
      const { data } = await axios.get(`${this.backendBaseUrl}/api/config`);
      this.config = data.data;
      this.error = data.success ? null : data.error || tr('errors.loadConfig');
      return this.config;
    },

    async refreshAll() {
      this.loading = true;
      try {
        await Promise.all([this.fetchStatus(), this.fetchConfig()]);
      } catch (error) {
        this.error = error?.message || tr('errors.connectBackend');
      } finally {
        this.loading = false;
      }
    },

    async saveConfig() {
      this.saving = true;
      try {
        const { data } = await axios.put(`${this.backendBaseUrl}/api/config`, this.config);
        if (!data.success) {
          throw new Error(data.error || tr('errors.saveConfig'));
        }

        this.config = data.data;
        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || tr('errors.saveConfig');
      } finally {
        this.saving = false;
      }
    },

    async startSession() {
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/session/start`, {
          label: this.manualLabel || null,
        });

        if (!data.success) {
          throw new Error(data.error || tr('errors.startSession'));
        }

        this.manualLabel = '';
        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || tr('errors.startSession');
      }
    },

    async stopSession(generateArtifacts = true) {
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/session/stop`, {
          generateArtifacts,
        });

        if (!data.success) {
          throw new Error(data.error || tr('errors.stopSession'));
        }

        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || tr('errors.stopSession');
      }
    },

    async generateArtifacts(sessionId = null) {
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/session/generate`, {
          sessionId,
        });

        if (!data.success) {
          throw new Error(data.error || tr('errors.generateArtifacts'));
        }

        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || tr('errors.generateArtifacts');
      }
    },

    async deleteSession(sessionId) {
      this.cleanupBusy = true;
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/session/delete`, {
          sessionId,
        });

        if (!data.success) {
          throw new Error(data.error || tr('errors.deleteSession'));
        }

        const deletedSessionCount = data.data?.deletedSessionCount || 0;
        const freedBytes = data.data?.freedBytes || 0;
        const remainingUsed = data.data?.storage?.pluginUsedBytes ?? 0;
        this.actionMessage =
          deletedSessionCount === 0
            ? tr('messages.noSessionsDeleted')
            : tr('messages.deletedSessions', {
                count: deletedSessionCount,
                suffix: deletedSessionCount === 1 ? '' : 's',
                freed: this.formatSize(freedBytes),
                remaining: this.formatSize(remainingUsed),
              });
        delete this.sessionDetailsById[sessionId];
        delete this.detailsLoadingById[sessionId];
        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || tr('errors.deleteSession');
      } finally {
        this.cleanupBusy = false;
      }
    },

    async deleteAllSessions() {
      this.cleanupBusy = true;
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/sessions/delete-all`, {});

        if (!data.success) {
          throw new Error(data.error || tr('errors.deleteAllSessions'));
        }

        const deletedSessionCount = data.data?.deletedSessionCount || 0;
        const freedBytes = data.data?.freedBytes || 0;
        const remainingUsed = data.data?.storage?.pluginUsedBytes ?? 0;
        this.actionMessage =
          deletedSessionCount === 0
            ? tr('messages.noSessionsDeleted')
            : tr('messages.deletedSessions', {
                count: deletedSessionCount,
                suffix: deletedSessionCount === 1 ? '' : 's',
                freed: this.formatSize(freedBytes),
                remaining: this.formatSize(remainingUsed),
              });
        this.sessionDetailsById = {};
        this.detailsLoadingById = {};
        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || tr('errors.deleteAllSessions');
      } finally {
        this.cleanupBusy = false;
      }
    },

    async fetchSessionDetails(sessionId) {
      if (!sessionId) {
        return null;
      }

      this.detailsLoadingById = {
        ...this.detailsLoadingById,
        [sessionId]: true,
      };

      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/session/details`, {
          sessionId,
        });

        if (!data.success) {
          throw new Error(data.error || tr('errors.loadSessionDetails'));
        }

        this.sessionDetailsById = {
          ...this.sessionDetailsById,
          [sessionId]: data.data,
        };
        this.error = null;
        return data.data;
      } catch (error) {
        this.error = error?.message || tr('errors.loadSessionDetails');
        return null;
      } finally {
        this.detailsLoadingById = {
          ...this.detailsLoadingById,
          [sessionId]: false,
        };
      }
    },

    async deleteArtifact(sessionId, relativePath) {
      this.cleanupBusy = true;
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/session/artifact/delete`, {
          sessionId,
          relativePath,
        });

        if (!data.success) {
          throw new Error(data.error || tr('errors.deleteArtifact'));
        }

        this.actionMessage = tr('messages.deletedArtifact', {
          freed: this.formatSize(data.data?.freedBytes || 0),
        });
        await Promise.all([this.fetchStatus(), this.fetchSessionDetails(sessionId)]);
        this.error = null;
      } catch (error) {
        this.error = error?.message || tr('errors.deleteArtifact');
      } finally {
        this.cleanupBusy = false;
      }
    },

    async deleteFrame(sessionId, relativePath) {
      this.cleanupBusy = true;
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/session/frame/delete`, {
          sessionId,
          relativePath,
        });

        if (!data.success) {
          throw new Error(data.error || tr('errors.deleteFrame'));
        }

        this.actionMessage = tr('messages.deletedFrame', {
          freed: this.formatSize(data.data?.freedBytes || 0),
        });
        await Promise.all([this.fetchStatus(), this.fetchSessionDetails(sessionId)]);
        this.error = null;
      } catch (error) {
        this.error = error?.message || tr('errors.deleteFrame');
      } finally {
        this.cleanupBusy = false;
      }
    },

    async updateBackend() {
      this.backendUpdateBusy = true;
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/backend/update`, {});

        if (!data.success) {
          throw new Error(data.error || tr('errors.startBackendUpdate'));
        }

        this.actionMessage = data.data?.message || tr('messages.backendUpdateStarted');
        this.error = null;
        return data.data;
      } catch (error) {
        this.error = error?.message || tr('errors.startBackendUpdate');
        return null;
      } finally {
        this.backendUpdateBusy = false;
      }
    },

    clearActionMessage() {
      this.actionMessage = null;
    },

    artifactUrl(relativePath) {
      if (!relativePath) {
        return null;
      }

      return `${this.backendBaseUrl}/media/${relativePath}`;
    },

    async downloadFile(relativePath, fallbackName = tr('common.download')) {
      const url = this.artifactUrl(relativePath);
      if (!url) {
        return;
      }

      try {
        const response = await fetch(url);
        if (!response.ok) {
          throw new Error(tr('errors.downloadFailed', { status: response.status }));
        }

        const blob = await response.blob();
        const objectUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = objectUrl;
        link.download = fallbackName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.setTimeout(() => {
          window.URL.revokeObjectURL(objectUrl);
        }, 1000);
        this.error = null;
      } catch (error) {
        this.error = error?.message || tr('errors.downloadFile');
      }
    },

    formatSize(bytes) {
      if (!bytes && bytes !== 0) {
        return '—';
      }

      const units = ['B', 'KB', 'MB', 'GB', 'TB'];
      let value = bytes;
      let unitIndex = 0;

      while (value >= 1024 && unitIndex < units.length - 1) {
        value /= 1024;
        unitIndex += 1;
      }

      const precision = unitIndex === 0 ? 0 : value >= 100 ? 0 : 1;
      return `${value.toFixed(precision)} ${units[unitIndex]}`;
    },

    startPolling() {
      this.stopPolling();
      this.pollTimer = setInterval(() => {
        this.fetchStatus().catch((error) => {
          this.error = error?.message || tr('errors.refreshStatus');
        });
      }, DEFAULT_POLL_INTERVAL_MS);
    },

    stopPolling() {
      if (this.pollTimer) {
        clearInterval(this.pollTimer);
        this.pollTimer = null;
      }
    },
  },
});
