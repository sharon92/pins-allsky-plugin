<template>
  <section class="rounded-2xl border border-gray-700 bg-gray-800/80 p-6 shadow-xl">
    <button
      type="button"
      class="flex w-full items-start justify-between gap-4 text-left"
      @click="$emit('toggle')"
    >
      <div>
        <h2 class="text-xl font-semibold text-white">
          {{ t('plugins.pinsAllSky.sections.recentSessions') }}
        </h2>
        <p class="mt-1 text-sm text-gray-400">
          {{ t('plugins.pinsAllSky.sections.recentSessionsDescription') }}
        </p>
      </div>
      <div class="flex items-center gap-3">
        <span
          class="rounded-full border border-gray-600 bg-gray-900/70 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-gray-300"
        >
          {{
            isOpen
              ? t('plugins.pinsAllSky.recentSessions.expanded')
              : t('plugins.pinsAllSky.recentSessions.collapsed')
          }}
        </span>
        <span
          class="flex h-9 w-9 items-center justify-center rounded-full border border-cyan-500/40 bg-cyan-500/10 text-lg font-semibold text-cyan-200"
        >
          {{ isOpen ? '-' : '+' }}
        </span>
      </div>
    </button>

    <div v-if="isOpen" class="mt-6 space-y-4">
      <div
        class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-900/50 px-4 py-3"
      >
        <div class="text-sm text-gray-300">
          <span :class="storage.withinLimit ? 'text-emerald-300' : 'text-amber-200'">
            {{
              storage.withinLimit || !storage.limitEnabled
                ? t('plugins.pinsAllSky.recentSessions.storageWithinLimit')
                : t('plugins.pinsAllSky.recentSessions.storageLimitExceeded')
            }}
          </span>
          <span class="text-gray-500">
            {{ t('plugins.pinsAllSky.recentSessions.pluginAvailable') }}
            {{
              storage.limitEnabled
                ? formatSize(storage.pluginAvailableBytes)
                : t('plugins.pinsAllSky.common.unlimited')
            }}.
          </span>
          <span class="text-gray-500">
            {{ t('plugins.pinsAllSky.recentSessions.piAvailable') }}
            {{ formatSize(storage.diskAvailableBytes) }}.
          </span>
        </div>
        <button
          class="inline-flex h-10 w-10 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
          :disabled="
            cleanupBusy ||
            status?.captureRunning ||
            status?.generateInProgress ||
            recentSessions.length === 0
          "
          :title="t('plugins.pinsAllSky.buttons.deleteAllSessions')"
          :aria-label="t('plugins.pinsAllSky.buttons.deleteAllSessions')"
          @click.stop="$emit('delete-all')"
        >
          <TrashIcon class="h-5 w-5" />
        </button>
      </div>
    </div>

    <div
      v-if="isOpen && recentSessions.length === 0"
      class="mt-6 rounded-xl border border-dashed border-gray-700 bg-gray-900/40 px-4 py-8 text-center text-sm text-gray-500"
    >
      {{ t('plugins.pinsAllSky.recentSessions.noSessions') }}
    </div>

    <div v-else-if="isOpen" class="mt-6 space-y-4">
      <PinsAllSkySessionCard
        v-for="session in recentSessions"
        :key="session.id"
        :session="session"
        :current-session-id="currentSessionId"
        :cleanup-busy="cleanupBusy"
        :status="status"
        :details="sessionDetailsById?.[session.id] || null"
        :details-loading="Boolean(detailsLoadingById?.[session.id])"
        :details-open="Boolean(sessionDetailOpen?.[session.id])"
        :format-date="formatDate"
        :format-count="formatCount"
        :format-size="formatSize"
        :describe-artifact="describeArtifact"
        :format-session-reason="formatSessionReason"
        @generate-artifacts="$emit('generate-artifacts', $event)"
        @delete-session="$emit('delete-session', $event)"
        @toggle-details="$emit('toggle-session-details', $event)"
        @refresh-session-details="$emit('refresh-session-details', $event)"
        @download-file="$emit('download-file', $event)"
        @delete-artifact="$emit('delete-artifact', $event)"
        @delete-frame="$emit('delete-frame', $event)"
      />
    </div>
  </section>
</template>

<script setup>
import { TrashIcon } from '@heroicons/vue/24/outline';
import { useI18n } from 'vue-i18n';
import PinsAllSkySessionCard from './PinsAllSkySessionCard.vue';

defineProps({
  isOpen: {
    type: Boolean,
    default: false,
  },
  storage: {
    type: Object,
    required: true,
  },
  recentSessions: {
    type: Array,
    default: () => [],
  },
  cleanupBusy: {
    type: Boolean,
    default: false,
  },
  status: {
    type: Object,
    default: null,
  },
  currentSessionId: {
    type: String,
    default: null,
  },
  sessionDetailsById: {
    type: Object,
    default: () => ({}),
  },
  detailsLoadingById: {
    type: Object,
    default: () => ({}),
  },
  sessionDetailOpen: {
    type: Object,
    default: () => ({}),
  },
  formatDate: {
    type: Function,
    required: true,
  },
  formatCount: {
    type: Function,
    required: true,
  },
  formatSize: {
    type: Function,
    required: true,
  },
  describeArtifact: {
    type: Function,
    required: true,
  },
  formatSessionReason: {
    type: Function,
    required: true,
  },
});

defineEmits([
  'toggle',
  'delete-all',
  'generate-artifacts',
  'delete-session',
  'toggle-session-details',
  'refresh-session-details',
  'download-file',
  'delete-artifact',
  'delete-frame',
]);

const { t } = useI18n({ useScope: 'global' });
</script>
