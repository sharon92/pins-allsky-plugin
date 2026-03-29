<template>
  <article class="rounded-2xl border border-gray-700 bg-gray-900/50 p-4">
    <div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
      <div class="space-y-1">
        <div class="text-lg font-semibold text-white">
          {{ session.label || session.id }}
        </div>
        <div class="text-sm text-gray-400">
          {{
            t('plugins.pinsAllSky.recentSessions.started', {
              date: formatDate(session.startedAtUtc),
            })
          }}
          <span v-if="session.endedAtUtc">
            •
            {{
              t('plugins.pinsAllSky.recentSessions.stopped', {
                date: formatDate(session.endedAtUtc),
              })
            }}</span
          >
        </div>
        <div class="text-sm text-gray-400">
          {{
            t('plugins.pinsAllSky.recentSessions.framesReason', {
              count: formatCount(session.captureCount),
              reason: formatSessionReason(session.startReason),
            })
          }}
          <span v-if="session.stopReason"> • {{ formatSessionReason(session.stopReason) }}</span>
        </div>
        <div class="text-sm text-gray-400">
          {{
            t('plugins.pinsAllSky.recentSessions.storage', {
              size: formatSize(session.totalSizeBytes),
            })
          }}
        </div>
      </div>

      <div class="flex flex-wrap gap-2">
        <button
          class="rounded-lg border border-cyan-500/40 bg-cyan-500/10 px-3 py-2 text-sm font-medium text-cyan-100 transition hover:bg-cyan-500/20 disabled:cursor-not-allowed disabled:opacity-40"
          :disabled="cleanupBusy"
          @click="$emit('generate-artifacts', session.id)"
        >
          {{ t('plugins.pinsAllSky.buttons.regenerate') }}
        </button>
        <button
          class="inline-flex h-10 w-10 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
          :disabled="
            cleanupBusy ||
            status?.captureRunning ||
            status?.generateInProgress ||
            session.id === currentSessionId
          "
          :title="t('plugins.pinsAllSky.buttons.deleteSession')"
          :aria-label="t('plugins.pinsAllSky.buttons.deleteSession')"
          @click="$emit('delete-session', session)"
        >
          <TrashIcon class="h-5 w-5" />
        </button>
        <button
          class="inline-flex h-10 items-center justify-center gap-2 rounded-lg border border-gray-600 bg-gray-800 px-3 py-2 text-sm text-white transition hover:border-cyan-400"
          :title="
            detailsOpen
              ? t('plugins.pinsAllSky.buttons.hideStoredFiles')
              : t('plugins.pinsAllSky.buttons.showStoredFiles')
          "
          :aria-label="
            detailsOpen
              ? t('plugins.pinsAllSky.buttons.hideStoredFiles')
              : t('plugins.pinsAllSky.buttons.showStoredFiles')
          "
          @click="$emit('toggle-details', session.id)"
        >
          <span>{{ t('plugins.pinsAllSky.buttons.files') }}</span>
          <ChevronUpIcon v-if="detailsOpen" class="h-4 w-4" />
          <ChevronDownIcon v-else class="h-4 w-4" />
        </button>
      </div>
    </div>

    <div class="mt-4 grid gap-3 sm:grid-cols-3">
      <div class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300">
        <div class="text-xs uppercase tracking-wide text-gray-500">
          {{ t('plugins.pinsAllSky.recentSessions.timelapse') }}
        </div>
        <div class="mt-2 flex items-start justify-between gap-3">
          <div>{{ describeArtifact(session.products?.timelapse) }}</div>
          <div v-if="session.products?.timelapse" class="flex items-center gap-2">
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
              :title="t('plugins.pinsAllSky.buttons.downloadTimelapse')"
              :aria-label="t('plugins.pinsAllSky.buttons.downloadTimelapse')"
              @click="
                $emit('download-file', { relativePath: session.products.timelapse.relativePath })
              "
            >
              <ArrowDownTrayIcon class="h-4 w-4" />
            </button>
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="
                cleanupBusy || status?.generateInProgress || session.id === currentSessionId
              "
              :title="t('plugins.pinsAllSky.buttons.deleteTimelapse')"
              :aria-label="t('plugins.pinsAllSky.buttons.deleteTimelapse')"
              @click="$emit('delete-artifact', { session, artifact: session.products.timelapse })"
            >
              <TrashIcon class="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>
      <div class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300">
        <div class="text-xs uppercase tracking-wide text-gray-500">
          {{ t('plugins.pinsAllSky.recentSessions.keogram') }}
        </div>
        <div class="mt-2 flex items-start justify-between gap-3">
          <div>{{ describeArtifact(session.products?.keogram) }}</div>
          <div v-if="session.products?.keogram" class="flex items-center gap-2">
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
              :title="t('plugins.pinsAllSky.buttons.downloadKeogram')"
              :aria-label="t('plugins.pinsAllSky.buttons.downloadKeogram')"
              @click="
                $emit('download-file', { relativePath: session.products.keogram.relativePath })
              "
            >
              <ArrowDownTrayIcon class="h-4 w-4" />
            </button>
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="
                cleanupBusy || status?.generateInProgress || session.id === currentSessionId
              "
              :title="t('plugins.pinsAllSky.buttons.deleteKeogram')"
              :aria-label="t('plugins.pinsAllSky.buttons.deleteKeogram')"
              @click="$emit('delete-artifact', { session, artifact: session.products.keogram })"
            >
              <TrashIcon class="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>
      <div class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300">
        <div class="text-xs uppercase tracking-wide text-gray-500">
          {{ t('plugins.pinsAllSky.recentSessions.startrails') }}
        </div>
        <div class="mt-2 flex items-start justify-between gap-3">
          <div>{{ describeArtifact(session.products?.startrails) }}</div>
          <div v-if="session.products?.startrails" class="flex items-center gap-2">
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
              :title="t('plugins.pinsAllSky.buttons.downloadStartrails')"
              :aria-label="t('plugins.pinsAllSky.buttons.downloadStartrails')"
              @click="
                $emit('download-file', { relativePath: session.products.startrails.relativePath })
              "
            >
              <ArrowDownTrayIcon class="h-4 w-4" />
            </button>
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="
                cleanupBusy || status?.generateInProgress || session.id === currentSessionId
              "
              :title="t('plugins.pinsAllSky.buttons.deleteStartrails')"
              :aria-label="t('plugins.pinsAllSky.buttons.deleteStartrails')"
              @click="$emit('delete-artifact', { session, artifact: session.products.startrails })"
            >
              <TrashIcon class="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="detailsOpen" class="mt-4 rounded-2xl border border-gray-700 bg-gray-800/50 p-4">
      <div class="mb-3 flex items-center justify-between gap-3">
        <div>
          <div class="text-sm font-semibold text-white">
            {{ t('plugins.pinsAllSky.sections.storedFrames') }}
          </div>
          <div class="text-xs text-gray-400">
            {{
              t('plugins.pinsAllSky.recentSessions.retainedFrames', {
                count: formatCount(details?.frames?.length ?? session.storedFrameCount ?? 0),
              })
            }}
          </div>
        </div>
        <button
          class="rounded-lg border border-gray-600 bg-gray-900/70 px-3 py-2 text-xs text-gray-200 transition hover:border-cyan-400"
          @click="$emit('refresh-session-details', session.id)"
        >
          {{ t('plugins.pinsAllSky.buttons.refreshFiles') }}
        </button>
      </div>

      <div
        v-if="detailsLoading"
        class="rounded-xl border border-dashed border-gray-700 bg-gray-900/40 px-4 py-6 text-center text-sm text-gray-500"
      >
        {{ t('plugins.pinsAllSky.recentSessions.loadingStoredFiles') }}
      </div>
      <div
        v-else-if="!details?.frames?.length"
        class="rounded-xl border border-dashed border-gray-700 bg-gray-900/40 px-4 py-6 text-center text-sm text-gray-500"
      >
        {{ t('plugins.pinsAllSky.recentSessions.noStoredFrames') }}
      </div>
      <div v-else class="max-h-80 space-y-2 overflow-y-auto pr-1">
        <div
          v-for="frame in details.frames"
          :key="frame.relativePath"
          class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2"
        >
          <div class="min-w-0">
            <div class="truncate text-sm text-white">{{ frame.name }}</div>
            <div class="text-xs text-gray-400">
              {{ formatDate(frame.capturedAtUtc) }} • {{ formatSize(frame.sizeBytes) }}
            </div>
          </div>
          <div class="flex items-center gap-2">
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
              :title="t('plugins.pinsAllSky.buttons.downloadItem', { name: frame.name })"
              :aria-label="t('plugins.pinsAllSky.buttons.downloadItem', { name: frame.name })"
              @click="
                $emit('download-file', {
                  relativePath: frame.relativePath,
                  fallbackName: frame.name,
                })
              "
            >
              <ArrowDownTrayIcon class="h-4 w-4" />
            </button>
            <button
              class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="
                cleanupBusy || status?.generateInProgress || session.id === currentSessionId
              "
              :title="t('plugins.pinsAllSky.buttons.deleteItem', { name: frame.name })"
              :aria-label="t('plugins.pinsAllSky.buttons.deleteItem', { name: frame.name })"
              @click="$emit('delete-frame', { session, frame })"
            >
              <TrashIcon class="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  </article>
</template>

<script setup>
import {
  ArrowDownTrayIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  TrashIcon,
} from '@heroicons/vue/24/outline';
import { useI18n } from 'vue-i18n';

defineProps({
  session: {
    type: Object,
    required: true,
  },
  currentSessionId: {
    type: String,
    default: null,
  },
  cleanupBusy: {
    type: Boolean,
    default: false,
  },
  status: {
    type: Object,
    default: null,
  },
  details: {
    type: Object,
    default: null,
  },
  detailsLoading: {
    type: Boolean,
    default: false,
  },
  detailsOpen: {
    type: Boolean,
    default: false,
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
  'generate-artifacts',
  'delete-session',
  'toggle-details',
  'refresh-session-details',
  'download-file',
  'delete-artifact',
  'delete-frame',
]);

const { t } = useI18n({ useScope: 'global' });
</script>
