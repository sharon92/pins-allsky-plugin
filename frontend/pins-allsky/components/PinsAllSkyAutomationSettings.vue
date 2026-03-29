<template>
  <div class="mt-2 flex w-full flex-wrap gap-4">
    <label
      :class="settingsFieldClass"
      class="rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2"
    >
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.automation.autoStartWithSequence') }}
      </span>
      <div class="flex items-center justify-between gap-3">
        <span class="text-sm text-gray-300">
          {{ t('plugins.pinsAllSky.modals.automation.sequenceTriggeredCapture') }}
        </span>
        <toggleButton
          :status-value="Boolean(config.autoStartWithSequence)"
          @update:statusValue="setRootConfigValue('autoStartWithSequence', $event)"
        />
      </div>
    </label>

    <div
      :class="settingsWideFieldClass"
      class="rounded-xl border border-cyan-500/20 bg-cyan-500/10 px-3 py-2 text-sm text-cyan-100"
    >
      {{ t('plugins.pinsAllSky.modals.automationInfo') }}
    </div>

    <label
      :class="settingsFieldClass"
      class="rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2"
    >
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.automation.advancedApiEnabled') }}
      </span>
      <div class="flex items-center justify-between gap-3">
        <span class="text-sm text-gray-300">
          {{ t('plugins.pinsAllSky.modals.automation.backendMonitoring') }}
        </span>
        <toggleButton
          :status-value="Boolean(config.advancedApi.enabled)"
          @update:statusValue="setAdvancedApiSetting('enabled', $event)"
        />
      </div>
    </label>

    <label :class="settingsFieldClass" class="block">
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.automation.protocol') }}
      </span>
      <input
        v-model="config.advancedApi.protocol"
        :title="t('plugins.pinsAllSky.modals.automation.protocolTooltip')"
        class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
      />
    </label>

    <label :class="settingsFieldClass" class="block">
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.automation.sequencePollIntervalSeconds') }}
      </span>
      <input
        v-model.number="config.sequencePollIntervalSeconds"
        type="number"
        min="5"
        max="300"
        :title="t('plugins.pinsAllSky.modals.automation.sequencePollTooltip')"
        class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
      />
      <span class="mt-2 block text-xs text-gray-500">
        {{ t('plugins.pinsAllSky.modals.automation.sequencePollIntervalHelp') }}
      </span>
    </label>

    <label :class="settingsFieldClass" class="block">
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.automation.advancedApiHost') }}
      </span>
      <input
        v-model="config.advancedApi.host"
        :title="t('plugins.pinsAllSky.modals.automation.advancedApiHostTooltip')"
        class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
      />
    </label>

    <label :class="settingsFieldClass" class="block">
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.automation.port') }}
      </span>
      <input
        v-model.number="config.advancedApi.port"
        type="number"
        :title="t('plugins.pinsAllSky.modals.automation.portTooltip')"
        class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
      />
    </label>

    <label :class="settingsFieldClass" class="block">
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.automation.timeoutSeconds') }}
      </span>
      <input
        v-model.number="config.advancedApi.requestTimeoutSeconds"
        type="number"
        min="1"
        max="30"
        :title="t('plugins.pinsAllSky.modals.automation.timeoutTooltip')"
        class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
      />
    </label>

    <label :class="settingsFieldClass" class="block">
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.automation.advancedApiBasePath') }}
      </span>
      <input
        v-model="config.advancedApi.basePath"
        :title="t('plugins.pinsAllSky.modals.automation.advancedApiBasePathTooltip')"
        class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
      />
    </label>

    <label :class="settingsFieldClass" class="block">
      <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
        {{ t('plugins.pinsAllSky.modals.automation.maxPluginStorageGb') }}
      </span>
      <input
        v-model.number="config.storage.maxUsageGb"
        type="number"
        min="0"
        step="0.1"
        inputmode="decimal"
        :title="t('plugins.pinsAllSky.modals.automation.maxPluginStorageTooltip')"
        class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
      />
      <span class="mt-2 block text-xs text-gray-500">
        {{ t('plugins.pinsAllSky.modals.automation.maxPluginStorageHelp') }}
      </span>
    </label>
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
  setRootConfigValue: {
    type: Function,
    required: true,
  },
  setAdvancedApiSetting: {
    type: Function,
    required: true,
  },
});

const { t } = useI18n({ useScope: 'global' });
const settingsFieldClass =
  'w-[calc(50%-0.5rem)] lg:w-[calc(33.333%-0.667rem)] 2xl:w-[calc(16.666%-0.834rem)]';
const settingsWideFieldClass =
  'w-full lg:w-[calc(66.666%-0.667rem)] 2xl:w-[calc(33.333%-0.667rem)]';
</script>
