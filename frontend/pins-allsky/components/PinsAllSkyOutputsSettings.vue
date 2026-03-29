<template>
  <div class="mt-2 w-full space-y-4">
    <label class="block rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2">
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.outputs.keepSourceFrames') }}
      </span>
      <div class="flex items-center justify-between gap-3">
        <span class="text-sm text-gray-300">
          {{ t('plugins.pinsAllSky.modals.outputs.retainOriginalCaptures') }}
        </span>
        <toggleButton
          :status-value="Boolean(config.products.keepFrames)"
          @update:statusValue="setProductSetting('keepFrames', $event)"
        />
      </div>
    </label>

    <div class="rounded-xl border border-gray-700 bg-gray-800/70 p-4">
      <div class="flex items-center justify-between gap-3">
        <span class="font-semibold text-white">
          {{ t('plugins.pinsAllSky.modals.outputs.timelapse') }}
        </span>
        <toggleButton
          :status-value="Boolean(config.products.timelapseEnabled)"
          @update:statusValue="setProductSetting('timelapseEnabled', $event)"
        />
      </div>
      <div v-if="config.products.timelapseEnabled" class="mt-4 flex flex-wrap gap-4">
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.fps') }}
          </span>
          <input
            v-model.number="config.products.timelapseFps"
            type="number"
            min="1"
            max="60"
            :title="t('plugins.pinsAllSky.modals.outputs.fpsTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.bitrateKbps') }}
          </span>
          <input
            v-model.number="config.products.timelapseBitrateKbps"
            type="number"
            min="1000"
            :title="t('plugins.pinsAllSky.modals.outputs.bitrateTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.width') }}
          </span>
          <input
            v-model.number="config.products.timelapseWidth"
            type="number"
            min="320"
            :title="t('plugins.pinsAllSky.modals.outputs.timelapseWidthTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.height') }}
          </span>
          <input
            v-model.number="config.products.timelapseHeight"
            type="number"
            min="240"
            :title="t('plugins.pinsAllSky.modals.outputs.timelapseHeightTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.codec') }}
          </span>
          <input
            v-model="config.products.timelapseCodec"
            :title="t('plugins.pinsAllSky.modals.outputs.codecTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.pixelFormat') }}
          </span>
          <input
            v-model="config.products.timelapsePixelFormat"
            :title="t('plugins.pinsAllSky.modals.outputs.pixelFormatTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.ffmpegLogLevel') }}
          </span>
          <input
            v-model="config.products.timelapseLogLevel"
            :title="t('plugins.pinsAllSky.modals.outputs.ffmpegLogLevelTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFullWidthClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.extraFfmpegArguments') }}
          </span>
          <textarea
            v-model="config.products.timelapseExtraParameters"
            rows="3"
            :title="t('plugins.pinsAllSky.modals.outputs.extraFfmpegArgumentsTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
      </div>
    </div>

    <div class="rounded-xl border border-gray-700 bg-gray-800/70 p-4">
      <div class="flex items-center justify-between gap-3">
        <span class="font-semibold text-white">
          {{ t('plugins.pinsAllSky.modals.outputs.keogram') }}
        </span>
        <toggleButton
          :status-value="Boolean(config.products.keogramEnabled)"
          @update:statusValue="setProductSetting('keogramEnabled', $event)"
        />
      </div>
      <div v-if="config.products.keogramEnabled" class="mt-4 flex flex-wrap gap-4">
        <label
          :class="settingsFieldClass"
          class="rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2"
        >
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.expandToFrameWidth') }}
          </span>
          <div class="flex items-center justify-between gap-3">
            <span class="text-sm text-gray-300">
              {{ t('plugins.pinsAllSky.modals.outputs.stretchOutput') }}
            </span>
            <toggleButton
              :status-value="Boolean(config.products.keogramExpand)"
              @update:statusValue="setProductSetting('keogramExpand', $event)"
            />
          </div>
        </label>
        <label
          :class="settingsFieldClass"
          class="rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2"
        >
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.showLabels') }}
          </span>
          <div class="flex items-center justify-between gap-3">
            <span class="text-sm text-gray-300">
              {{ t('plugins.pinsAllSky.modals.outputs.drawCaptions') }}
            </span>
            <toggleButton
              :status-value="Boolean(config.products.keogramShowLabels)"
              @update:statusValue="setProductSetting('keogramShowLabels', $event)"
            />
          </div>
        </label>
        <label
          :class="settingsFieldClass"
          class="rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2"
        >
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.showDate') }}
          </span>
          <div class="flex items-center justify-between gap-3">
            <span class="text-sm text-gray-300">
              {{ t('plugins.pinsAllSky.modals.outputs.stampDate') }}
            </span>
            <toggleButton
              :status-value="Boolean(config.products.keogramShowDate)"
              @update:statusValue="setProductSetting('keogramShowDate', $event)"
            />
          </div>
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.rotateDegrees') }}
          </span>
          <input
            v-model.number="config.products.keogramRotateDegrees"
            type="number"
            inputmode="decimal"
            :title="t('plugins.pinsAllSky.modals.outputs.rotateTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.fontName') }}
          </span>
          <input
            v-model="config.products.keogramFontName"
            :title="t('plugins.pinsAllSky.modals.outputs.fontNameTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.fontColor') }}
          </span>
          <input
            v-model="config.products.keogramFontColor"
            :title="t('plugins.pinsAllSky.modals.outputs.fontColorTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.fontSize') }}
          </span>
          <input
            v-model.number="config.products.keogramFontSize"
            type="number"
            min="0.1"
            step="0.1"
            inputmode="decimal"
            :title="t('plugins.pinsAllSky.modals.outputs.fontSizeTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.lineThickness') }}
          </span>
          <input
            v-model.number="config.products.keogramLineThickness"
            type="number"
            min="1"
            :title="t('plugins.pinsAllSky.modals.outputs.lineThicknessTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <label :class="settingsFullWidthClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.extraKeogramArguments') }}
          </span>
          <textarea
            v-model="config.products.keogramExtraParameters"
            rows="3"
            :title="t('plugins.pinsAllSky.modals.outputs.extraKeogramArgumentsTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
      </div>
    </div>

    <div class="rounded-xl border border-gray-700 bg-gray-800/70 p-4">
      <div class="flex items-center justify-between gap-3">
        <span class="font-semibold text-white">
          {{ t('plugins.pinsAllSky.modals.outputs.startrails') }}
        </span>
        <toggleButton
          :status-value="Boolean(config.products.startrailsEnabled)"
          @update:statusValue="setProductSetting('startrailsEnabled', $event)"
        />
      </div>
      <div v-if="config.products.startrailsEnabled" class="mt-4 flex flex-wrap gap-4">
        <label :class="settingsFieldClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.brightnessThreshold') }}
          </span>
          <input
            v-model.number="config.products.startrailsBrightnessThreshold"
            type="number"
            min="0"
            max="1"
            step="0.01"
            inputmode="decimal"
            :title="t('plugins.pinsAllSky.modals.outputs.brightnessThresholdTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
        <div
          :class="settingsWideFieldClass"
          class="rounded-xl border border-amber-500/20 bg-amber-500/10 px-3 py-2 text-sm text-amber-100"
        >
          {{ t('plugins.pinsAllSky.modals.startrailMountInfo') }}
        </div>
        <label :class="settingsFullWidthClass" class="block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.outputs.extraStartrailsArguments') }}
          </span>
          <textarea
            v-model="config.products.startrailsExtraParameters"
            rows="3"
            :title="t('plugins.pinsAllSky.modals.outputs.extraStartrailsArgumentsTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
          />
        </label>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useI18n } from 'vue-i18n';
import toggleButton from '@/components/helpers/toggleButton.vue';

defineProps({
  config: {
    type: Object,
    required: true,
  },
  setProductSetting: {
    type: Function,
    required: true,
  },
});

const { t } = useI18n({ useScope: 'global' });
const settingsFieldClass =
  'w-[calc(50%-0.5rem)] lg:w-[calc(33.333%-0.667rem)] 2xl:w-[calc(16.666%-0.834rem)]';
const settingsWideFieldClass =
  'w-full lg:w-[calc(66.666%-0.667rem)] 2xl:w-[calc(33.333%-0.667rem)]';
const settingsFullWidthClass = 'w-full';
</script>
