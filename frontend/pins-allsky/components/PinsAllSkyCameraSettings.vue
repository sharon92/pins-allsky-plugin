<template>
  <div class="mt-2 w-full space-y-4">
    <div class="flex flex-wrap gap-4">
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.intervalSeconds') }}
        </span>
        <input
          v-model.number="config.camera.intervalSeconds"
          type="number"
          min="5"
          :title="t('plugins.pinsAllSky.modals.camera.intervalTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.timeoutSeconds') }}
        </span>
        <input
          v-model.number="config.camera.captureTimeoutSeconds"
          type="number"
          min="15"
          :title="t('plugins.pinsAllSky.modals.camera.timeoutTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.width') }}
        </span>
        <input
          v-model.number="config.camera.width"
          type="number"
          min="640"
          :title="t('plugins.pinsAllSky.modals.camera.widthTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.height') }}
        </span>
        <input
          v-model.number="config.camera.height"
          type="number"
          min="480"
          :title="t('plugins.pinsAllSky.modals.camera.heightTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.jpegQuality') }}
        </span>
        <input
          v-model.number="config.camera.quality"
          type="number"
          min="1"
          max="100"
          :title="t('plugins.pinsAllSky.modals.camera.jpegQualityTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.warmupMilliseconds') }}
        </span>
        <input
          v-model.number="config.camera.warmupMilliseconds"
          type="number"
          min="1"
          :title="t('plugins.pinsAllSky.modals.camera.warmupTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.metering') }}
        </span>
        <select
          v-model="config.camera.meteringMode"
          :title="t('plugins.pinsAllSky.modals.camera.meteringTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        >
          <option v-for="option in meteringOptions" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.awb') }}
        </span>
        <select
          v-model="config.camera.awbMode"
          :title="t('plugins.pinsAllSky.modals.camera.awbTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        >
          <option v-for="option in awbOptions" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.denoise') }}
        </span>
        <select
          v-model="config.camera.denoiseMode"
          :title="t('plugins.pinsAllSky.modals.camera.denoiseTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        >
          <option v-for="option in denoiseOptions" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.evCompensation') }}
        </span>
        <input
          v-model.number="config.camera.evCompensation"
          type="number"
          step="0.1"
          inputmode="decimal"
          :title="t('plugins.pinsAllSky.modals.camera.evCompensationTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.rotation') }}
        </span>
        <input
          v-model.number="config.camera.rotation"
          type="number"
          min="0"
          max="180"
          :title="t('plugins.pinsAllSky.modals.camera.rotationTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.brightness') }}
        </span>
        <input
          v-model.number="config.camera.brightness"
          type="number"
          step="0.1"
          inputmode="decimal"
          :title="t('plugins.pinsAllSky.modals.camera.brightnessTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.contrast') }}
        </span>
        <input
          v-model.number="config.camera.contrast"
          type="number"
          min="0"
          step="0.1"
          inputmode="decimal"
          :title="t('plugins.pinsAllSky.modals.camera.contrastTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.saturation') }}
        </span>
        <input
          v-model.number="config.camera.saturation"
          type="number"
          min="0"
          step="0.1"
          inputmode="decimal"
          :title="t('plugins.pinsAllSky.modals.camera.saturationTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
      <label :class="settingsFieldClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.sharpness') }}
        </span>
        <input
          v-model.number="config.camera.sharpness"
          type="number"
          min="0"
          step="0.1"
          inputmode="decimal"
          :title="t('plugins.pinsAllSky.modals.camera.sharpnessTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
    </div>

    <div class="flex flex-wrap gap-4">
      <div
        class="w-full rounded-xl border border-gray-700 bg-gray-800/70 p-3 lg:w-[calc(50%-0.5rem)]"
      >
        <label class="flex items-center justify-between gap-3">
          <span class="text-sm text-gray-300">
            {{ t('plugins.pinsAllSky.modals.camera.manualExposure') }}
          </span>
          <toggleButton
            :status-value="Boolean(config.camera.useManualExposure)"
            @update:statusValue="setCameraSetting('useManualExposure', $event)"
          />
        </label>
        <label class="mt-3 block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.camera.shutterMicroseconds') }}
          </span>
          <input
            v-model.number="config.camera.shutterMicroseconds"
            type="number"
            min="1"
            inputmode="numeric"
            :disabled="!config.camera.useManualExposure"
            :title="t('plugins.pinsAllSky.modals.camera.shutterTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400 disabled:cursor-not-allowed disabled:opacity-50"
          />
        </label>
        <p class="mt-3 text-xs text-gray-500">
          {{ t('plugins.pinsAllSky.modals.camera.manualExposureHelp') }}
        </p>
      </div>

      <div
        class="w-full rounded-xl border border-gray-700 bg-gray-800/70 p-3 lg:w-[calc(50%-0.5rem)]"
      >
        <label class="flex items-center justify-between gap-3">
          <span class="text-sm text-gray-300">
            {{ t('plugins.pinsAllSky.modals.camera.manualGain') }}
          </span>
          <toggleButton
            :status-value="Boolean(config.camera.useManualGain)"
            @update:statusValue="setCameraSetting('useManualGain', $event)"
          />
        </label>
        <label class="mt-3 block">
          <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
            {{ t('plugins.pinsAllSky.modals.camera.analogGain') }}
          </span>
          <input
            v-model.number="config.camera.analogGain"
            type="number"
            min="1"
            step="0.1"
            inputmode="decimal"
            :disabled="!config.camera.useManualGain"
            :title="t('plugins.pinsAllSky.modals.camera.analogGainTooltip')"
            class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400 disabled:cursor-not-allowed disabled:opacity-50"
          />
        </label>
        <p class="mt-3 text-xs text-gray-500">
          {{ t('plugins.pinsAllSky.modals.camera.manualGainHelp') }}
        </p>
      </div>
    </div>

    <div class="flex flex-wrap gap-4">
      <div class="w-full rounded-xl border border-cyan-500/20 bg-cyan-500/10 p-4">
        <div class="text-sm font-semibold text-cyan-100">
          {{ t('plugins.pinsAllSky.modals.camera.allSkyAutoExposure') }}
        </div>
        <p class="mt-2 text-sm text-cyan-50/90">
          {{ t('plugins.pinsAllSky.modals.camera.allSkyAutoExposureHelp') }}
        </p>

        <div class="mt-4 flex flex-wrap gap-4">
          <label :class="settingsFieldClass" class="block">
            <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
              {{ t('plugins.pinsAllSky.modals.camera.autoMeanTarget') }}
            </span>
            <input
              v-model.number="config.camera.autoMeanTarget"
              type="number"
              min="0"
              max="1"
              step="0.01"
              inputmode="decimal"
              :title="t('plugins.pinsAllSky.modals.camera.autoMeanTargetTooltip')"
              class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
            />
          </label>
          <label :class="settingsFieldClass" class="block">
            <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
              {{ t('plugins.pinsAllSky.modals.camera.autoMeanThreshold') }}
            </span>
            <input
              v-model.number="config.camera.autoMeanThreshold"
              type="number"
              min="0"
              max="1"
              step="0.01"
              inputmode="decimal"
              :title="t('plugins.pinsAllSky.modals.camera.autoMeanThresholdTooltip')"
              class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
            />
          </label>
          <label :class="settingsFieldClass" class="block">
            <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
              {{ t('plugins.pinsAllSky.modals.camera.autoMaxExposureMicroseconds') }}
            </span>
            <input
              v-model.number="config.camera.autoMaxExposureMicroseconds"
              type="number"
              min="1"
              inputmode="numeric"
              :title="t('plugins.pinsAllSky.modals.camera.autoMaxExposureTooltip')"
              class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
            />
          </label>
          <label :class="settingsFieldClass" class="block">
            <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
              {{ t('plugins.pinsAllSky.modals.camera.autoMaxGain') }}
            </span>
            <input
              v-model.number="config.camera.autoMaxGain"
              type="number"
              min="1"
              max="64"
              step="0.1"
              inputmode="decimal"
              :title="t('plugins.pinsAllSky.modals.camera.autoMaxGainTooltip')"
              class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
            />
          </label>
          <label :class="settingsFieldClass" class="block">
            <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
              {{ t('plugins.pinsAllSky.modals.camera.autoMeanP0') }}
            </span>
            <input
              v-model.number="config.camera.autoMeanP0"
              type="number"
              min="0"
              step="0.1"
              inputmode="decimal"
              :title="t('plugins.pinsAllSky.modals.camera.autoMeanP0Tooltip')"
              class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
            />
          </label>
          <label :class="settingsFieldClass" class="block">
            <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
              {{ t('plugins.pinsAllSky.modals.camera.autoMeanP1') }}
            </span>
            <input
              v-model.number="config.camera.autoMeanP1"
              type="number"
              min="0"
              step="0.1"
              inputmode="decimal"
              :title="t('plugins.pinsAllSky.modals.camera.autoMeanP1Tooltip')"
              class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
            />
          </label>
          <label :class="settingsFieldClass" class="block">
            <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
              {{ t('plugins.pinsAllSky.modals.camera.autoMeanP2') }}
            </span>
            <input
              v-model.number="config.camera.autoMeanP2"
              type="number"
              min="0"
              step="0.1"
              inputmode="decimal"
              :title="t('plugins.pinsAllSky.modals.camera.autoMeanP2Tooltip')"
              class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
            />
          </label>
        </div>
      </div>

      <div
        :class="settingsFullWidthClass"
        class="rounded-xl border border-cyan-500/20 bg-cyan-500/10 px-3 py-2 text-sm text-cyan-100"
      >
        {{ t('plugins.pinsAllSky.modals.cameraRotationInfo') }}
      </div>
      <label
        :class="settingsFieldClass"
        class="rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2"
      >
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.horizontalFlip') }}
        </span>
        <div class="flex items-center justify-between gap-3">
          <span class="text-sm text-gray-300">
            {{ t('plugins.pinsAllSky.modals.camera.mirrorOnXAxis') }}
          </span>
          <toggleButton
            :status-value="Boolean(config.camera.horizontalFlip)"
            @update:statusValue="setCameraSetting('horizontalFlip', $event)"
          />
        </div>
      </label>
      <label
        :class="settingsFieldClass"
        class="rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2"
      >
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.verticalFlip') }}
        </span>
        <div class="flex items-center justify-between gap-3">
          <span class="text-sm text-gray-300">
            {{ t('plugins.pinsAllSky.modals.camera.mirrorOnYAxis') }}
          </span>
          <toggleButton
            :status-value="Boolean(config.camera.verticalFlip)"
            @update:statusValue="setCameraSetting('verticalFlip', $event)"
          />
        </div>
      </label>
      <label :class="settingsFullWidthClass" class="block">
        <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
          {{ t('plugins.pinsAllSky.modals.camera.extraArguments') }}
        </span>
        <textarea
          v-model="config.camera.extraArguments"
          rows="3"
          :title="t('plugins.pinsAllSky.modals.camera.extraArgumentsTooltip')"
          class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
        />
      </label>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import toggleButton from '@/components/helpers/toggleButton.vue';

defineProps({
  config: {
    type: Object,
    required: true,
  },
  setCameraSetting: {
    type: Function,
    required: true,
  },
});

const { t } = useI18n({ useScope: 'global' });
const settingsFieldClass =
  'w-[calc(50%-0.5rem)] lg:w-[calc(33.333%-0.667rem)] 2xl:w-[calc(16.666%-0.834rem)]';
const settingsFullWidthClass = 'w-full';

const meteringOptions = computed(() => [
  { value: 'centre', label: t('plugins.pinsAllSky.modals.camera.options.centre') },
  { value: 'spot', label: t('plugins.pinsAllSky.modals.camera.options.spot') },
  { value: 'average', label: t('plugins.pinsAllSky.modals.camera.options.average') },
  { value: 'custom', label: t('plugins.pinsAllSky.modals.camera.options.custom') },
]);

const awbOptions = computed(() => [
  { value: 'auto', label: t('plugins.pinsAllSky.modals.camera.options.auto') },
  {
    value: 'incandescent',
    label: t('plugins.pinsAllSky.modals.camera.options.incandescent'),
  },
  { value: 'tungsten', label: t('plugins.pinsAllSky.modals.camera.options.tungsten') },
  {
    value: 'fluorescent',
    label: t('plugins.pinsAllSky.modals.camera.options.fluorescent'),
  },
  { value: 'indoor', label: t('plugins.pinsAllSky.modals.camera.options.indoor') },
  { value: 'daylight', label: t('plugins.pinsAllSky.modals.camera.options.daylight') },
  { value: 'cloudy', label: t('plugins.pinsAllSky.modals.camera.options.cloudy') },
  { value: 'custom', label: t('plugins.pinsAllSky.modals.camera.options.custom') },
]);

const denoiseOptions = computed(() => [
  { value: 'auto', label: t('plugins.pinsAllSky.modals.camera.options.auto') },
  { value: 'off', label: t('plugins.pinsAllSky.modals.camera.options.off') },
  { value: 'cdn_off', label: t('plugins.pinsAllSky.modals.camera.options.cdnOff') },
  { value: 'cdn_fast', label: t('plugins.pinsAllSky.modals.camera.options.cdnFast') },
  { value: 'cdn_hq', label: t('plugins.pinsAllSky.modals.camera.options.cdnHq') },
]);
</script>
