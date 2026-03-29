<template>
  <section class="rounded-2xl border border-gray-700 bg-gray-800/80 p-5 shadow-xl">
    <h2 class="text-xl font-semibold text-white">
      {{ t('plugins.pinsAllSky.sections.sessionControl') }}
    </h2>

    <div class="mt-5 space-y-5">
      <div class="flex flex-wrap items-stretch gap-2">
        <input
          :value="manualLabel"
          :title="t('plugins.pinsAllSky.estimate.sessionLabelTitle', { label: defaultManualLabel })"
          class="min-w-0 flex-1 rounded-xl border border-gray-600 bg-gray-900/80 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          :placeholder="t('plugins.pinsAllSky.estimate.sessionLabelPlaceholder')"
          @input="$emit('update:manualLabel', $event.target.value)"
        />

        <div class="flex items-center gap-2">
          <button
            type="button"
            :title="t('plugins.pinsAllSky.buttons.startCapture')"
            :aria-label="t('plugins.pinsAllSky.buttons.startCapture')"
            class="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-600 text-white transition hover:bg-emerald-500 disabled:cursor-not-allowed disabled:opacity-40"
            :disabled="loading || status?.captureRunning"
            @click="$emit('start-session')"
          >
            <PlayIcon class="h-5 w-5" />
          </button>
          <button
            type="button"
            :title="
              status?.generateInProgress
                ? t('plugins.pinsAllSky.buttons.renderingProgress')
                : t('plugins.pinsAllSky.buttons.renderProgress')
            "
            :aria-label="
              status?.generateInProgress
                ? t('plugins.pinsAllSky.buttons.renderingProgress')
                : t('plugins.pinsAllSky.buttons.renderProgress')
            "
            class="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20 disabled:cursor-not-allowed disabled:opacity-40"
            :disabled="
              loading ||
              !status?.captureRunning ||
              !currentSession?.captureCount ||
              status?.generateInProgress
            "
            @click="$emit('generate-artifacts')"
          >
            <ArrowDownTrayIcon
              class="h-5 w-5"
              :class="status?.generateInProgress ? 'animate-pulse' : ''"
            />
          </button>
          <button
            type="button"
            :title="t('plugins.pinsAllSky.buttons.stopAndRender')"
            :aria-label="t('plugins.pinsAllSky.buttons.stopAndRender')"
            class="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-rose-600 text-white transition hover:bg-rose-500 disabled:cursor-not-allowed disabled:opacity-40"
            :disabled="loading || !status?.captureRunning"
            @click="$emit('stop-session')"
          >
            <StopIcon class="h-5 w-5" />
          </button>
        </div>
      </div>

      <div class="overflow-hidden rounded-2xl border border-gray-700 bg-black/60">
        <div class="relative h-[28rem] w-full">
          <img
            v-if="currentImageUrl"
            :src="currentImageUrl"
            alt="Latest all-sky frame"
            class="h-full w-full object-contain"
          />
          <div
            v-else
            class="flex h-full items-center justify-center px-6 text-center text-sm text-gray-500"
          >
            {{ t('plugins.pinsAllSky.preview.noFrameYet') }}
          </div>

          <div
            class="pointer-events-none absolute inset-x-0 top-0 h-24 bg-gradient-to-b from-black/80 via-black/30 to-transparent"
          />
          <div
            class="pointer-events-none absolute inset-x-0 bottom-0 h-32 bg-gradient-to-t from-black/85 via-black/35 to-transparent"
          />

          <div class="absolute right-4 top-4 z-10 flex items-center gap-2">
            <div
              class="pointer-events-none rounded-xl border border-white/10 bg-black/45 px-3 py-2 text-[11px] font-semibold uppercase tracking-[0.2em] text-gray-300 backdrop-blur-sm"
            >
              {{ t('plugins.pinsAllSky.sections.livePreview') }}
            </div>
            <button
              type="button"
              :title="
                loading
                  ? t('plugins.pinsAllSky.buttons.refreshingPreview')
                  : t('plugins.pinsAllSky.buttons.refreshPreview')
              "
              :aria-label="
                loading
                  ? t('plugins.pinsAllSky.buttons.refreshingPreview')
                  : t('plugins.pinsAllSky.buttons.refreshPreview')
              "
              class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-white/10 bg-black/45 text-gray-200 backdrop-blur-sm transition hover:border-cyan-400 hover:text-white disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="loading"
              @click="$emit('refresh-all')"
            >
              <ArrowPathIcon class="h-5 w-5" :class="loading ? 'animate-spin' : ''" />
            </button>
          </div>

          <div
            class="pointer-events-none absolute left-4 top-4 rounded-xl border border-white/10 bg-black/45 px-3 py-2 backdrop-blur-sm"
          >
            <div class="text-[11px] font-semibold uppercase tracking-[0.2em] text-gray-300">
              {{ t('plugins.pinsAllSky.preview.session') }}
            </div>
            <div class="mt-1 text-sm font-medium text-white">
              {{ currentSession?.id || t('plugins.pinsAllSky.preview.noActiveSession') }}
            </div>
          </div>

          <div
            class="pointer-events-none absolute bottom-4 left-4 rounded-xl border border-white/10 bg-black/45 px-3 py-2 backdrop-blur-sm"
          >
            <div class="space-y-1 text-sm text-white">
              <div>
                <span class="text-gray-300">{{ t('plugins.pinsAllSky.preview.lastCapture') }}</span>
                {{ formatDate(currentSession?.lastCaptureAtUtc) }}
              </div>
              <div>
                <span class="text-gray-300">{{ t('plugins.pinsAllSky.preview.frameCount') }}</span>
                {{ formatCount(currentSession?.captureCount || 0) }}
              </div>
            </div>
          </div>

          <div
            class="pointer-events-none absolute bottom-4 right-4 rounded-xl border border-white/10 bg-black/45 px-3 py-2 backdrop-blur-sm"
          >
            <div class="space-y-1 text-right text-sm text-white">
              <div>
                <span class="text-gray-300">{{
                  t('plugins.pinsAllSky.preview.captureInterval')
                }}</span>
                {{ formatInterval(config?.camera?.intervalSeconds) }}
              </div>
              <div>
                <span class="text-gray-300">{{ t('plugins.pinsAllSky.preview.exposure') }}</span>
                {{ formatExposure(config?.camera) }}
              </div>
              <div>
                <span class="text-gray-300">{{ t('plugins.pinsAllSky.preview.gain') }}</span>
                {{ formatGain(config?.camera) }}
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="rounded-2xl border border-gray-700 bg-gray-900/50 p-4">
        <div class="text-sm font-semibold text-white">
          {{ t('plugins.pinsAllSky.sections.sessionEstimate') }}
        </div>
        <div class="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
          <label class="block rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <span class="block text-xs uppercase tracking-wide text-gray-500">{{
              t('plugins.pinsAllSky.estimate.start')
            }}</span>
            <input
              :value="estimateStartLocal"
              type="datetime-local"
              :title="t('plugins.pinsAllSky.estimate.startTooltip')"
              class="mt-2 w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              @input="$emit('update:estimateStartLocal', $event.target.value)"
            />
          </label>
          <label class="block rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <span class="block text-xs uppercase tracking-wide text-gray-500">{{
              t('plugins.pinsAllSky.estimate.end')
            }}</span>
            <input
              :value="estimateEndLocal"
              type="datetime-local"
              :title="t('plugins.pinsAllSky.estimate.endTooltip')"
              class="mt-2 w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              @input="$emit('update:estimateEndLocal', $event.target.value)"
            />
          </label>
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-500">
              {{ t('plugins.pinsAllSky.estimate.expectedDuration') }}
            </div>
            <div class="mt-2 text-sm text-white">{{ estimateDurationLabel }}</div>
          </div>
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-500">
              {{ t('plugins.pinsAllSky.estimate.expectedFrames') }}
            </div>
            <div class="mt-2 text-sm text-white">{{ formatCount(estimatedFrameCount) }}</div>
          </div>
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-500">
              {{ t('plugins.pinsAllSky.estimate.expectedStorage') }}
            </div>
            <div
              class="mt-2 text-sm font-semibold"
              :class="estimateExceedsAvailable ? 'text-amber-300' : 'text-white'"
            >
              {{ formatSize(estimatedStorageBytes) }}
            </div>
          </div>
        </div>

        <div
          v-if="estimateWarning"
          class="mt-3 rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-100"
        >
          {{ estimateWarning }}
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ArrowDownTrayIcon, ArrowPathIcon, PlayIcon, StopIcon } from '@heroicons/vue/24/outline';
import { useI18n } from 'vue-i18n';

defineProps({
  manualLabel: {
    type: String,
    default: '',
  },
  defaultManualLabel: {
    type: String,
    required: true,
  },
  loading: {
    type: Boolean,
    default: false,
  },
  status: {
    type: Object,
    default: null,
  },
  currentSession: {
    type: Object,
    default: null,
  },
  currentImageUrl: {
    type: String,
    default: null,
  },
  config: {
    type: Object,
    default: null,
  },
  estimateStartLocal: {
    type: String,
    default: '',
  },
  estimateEndLocal: {
    type: String,
    default: '',
  },
  estimateDurationLabel: {
    type: String,
    required: true,
  },
  estimatedFrameCount: {
    type: Number,
    default: 0,
  },
  estimatedStorageBytes: {
    type: Number,
    default: 0,
  },
  estimateExceedsAvailable: {
    type: Boolean,
    default: false,
  },
  estimateWarning: {
    type: String,
    default: null,
  },
  formatDate: {
    type: Function,
    required: true,
  },
  formatInterval: {
    type: Function,
    required: true,
  },
  formatExposure: {
    type: Function,
    required: true,
  },
  formatGain: {
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
});

defineEmits([
  'update:manualLabel',
  'update:estimateStartLocal',
  'update:estimateEndLocal',
  'start-session',
  'generate-artifacts',
  'stop-session',
  'refresh-all',
]);

const { t } = useI18n({ useScope: 'global' });
</script>
