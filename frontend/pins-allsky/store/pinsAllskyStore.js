import axios from 'axios';
import { defineStore } from 'pinia';
import { useSettingsStore } from '@/store/settingsStore';

const DEFAULT_BACKEND_PORT = 19091;
const DEFAULT_POLL_INTERVAL_MS = 5000;

export const usePinsAllSkyStore = defineStore('pinsAllSkyStore', {
  state: () => ({
    backendPort: DEFAULT_BACKEND_PORT,
    status: null,
    config: null,
    error: null,
    loading: false,
    saving: false,
    manualLabel: '',
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
      this.error = data.success ? null : data.error || 'Unable to load backend status.';
      this.imageNonce = Date.now();
      return this.status;
    },

    async fetchConfig() {
      const { data } = await axios.get(`${this.backendBaseUrl}/api/config`);
      this.config = data.data;
      this.error = data.success ? null : data.error || 'Unable to load configuration.';
      return this.config;
    },

    async refreshAll() {
      this.loading = true;
      try {
        await Promise.all([this.fetchStatus(), this.fetchConfig()]);
      } catch (error) {
        this.error = error?.message || 'Unable to connect to the AllSky backend.';
      } finally {
        this.loading = false;
      }
    },

    async saveConfig() {
      this.saving = true;
      try {
        const { data } = await axios.put(`${this.backendBaseUrl}/api/config`, this.config);
        if (!data.success) {
          throw new Error(data.error || 'Saving configuration failed.');
        }

        this.config = data.data;
        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || 'Saving configuration failed.';
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
          throw new Error(data.error || 'Unable to start session.');
        }

        this.manualLabel = '';
        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || 'Unable to start session.';
      }
    },

    async stopSession(generateArtifacts = true) {
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/session/stop`, {
          generateArtifacts,
        });

        if (!data.success) {
          throw new Error(data.error || 'Unable to stop session.');
        }

        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || 'Unable to stop session.';
      }
    },

    async generateArtifacts(sessionId = null) {
      try {
        const { data } = await axios.post(`${this.backendBaseUrl}/api/session/generate`, {
          sessionId,
        });

        if (!data.success) {
          throw new Error(data.error || 'Unable to generate artifacts.');
        }

        await this.fetchStatus();
        this.error = null;
      } catch (error) {
        this.error = error?.message || 'Unable to generate artifacts.';
      }
    },

    artifactUrl(relativePath) {
      if (!relativePath) {
        return null;
      }

      return `${this.backendBaseUrl}/media/${relativePath}`;
    },

    startPolling() {
      this.stopPolling();
      this.pollTimer = setInterval(() => {
        this.fetchStatus().catch((error) => {
          this.error = error?.message || 'Unable to refresh backend status.';
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
