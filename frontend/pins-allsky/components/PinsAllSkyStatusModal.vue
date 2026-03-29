<template>
  <Modal :show="show" max-width="max-w-2xl" @close="$emit('close')">
    <template #header>
      <h2 class="text-xl font-semibold text-white">
        {{ t('plugins.pinsAllSky.statusModal.title') }}
      </h2>
    </template>

    <template #body>
      <div class="w-full">
        <div
          class="grid grid-cols-[auto_minmax(0,1fr)] gap-x-8 gap-y-3 lg:grid-cols-[auto_minmax(0,1fr)_auto_minmax(0,1fr)]"
        >
          <template v-for="item in statusRows" :key="item.label">
            <div class="text-xs font-semibold uppercase tracking-wide text-gray-500">
              {{ item.label }}
            </div>
            <div class="text-sm" :class="item.className">
              {{ item.value }}
            </div>
          </template>
        </div>

        <div class="mt-6 flex flex-wrap items-center justify-end gap-3">
          <button
            type="button"
            class="rounded-xl border border-cyan-500/40 bg-cyan-500/10 px-4 py-2 text-sm font-semibold text-cyan-100 transition hover:bg-cyan-500/20 disabled:cursor-not-allowed disabled:opacity-40"
            :disabled="disableUpdate"
            :title="t('plugins.pinsAllSky.statusModal.updateBackendTooltip')"
            @click="$emit('update-backend')"
          >
            {{
              backendUpdateBusy
                ? t('plugins.pinsAllSky.buttons.startingUpdate')
                : t('plugins.pinsAllSky.buttons.updateBackend')
            }}
          </button>
          <button
            type="button"
            class="rounded-xl bg-cyan-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-cyan-500"
            @click="$emit('close')"
          >
            {{ t('plugins.pinsAllSky.common.ok') }}
          </button>
        </div>
      </div>
    </template>
  </Modal>
</template>

<script setup>
import { useI18n } from 'vue-i18n';
import Modal from '@/components/helpers/Modal.vue';

defineProps({
  show: {
    type: Boolean,
    default: false,
  },
  statusRows: {
    type: Array,
    default: () => [],
  },
  backendUpdateBusy: {
    type: Boolean,
    default: false,
  },
  disableUpdate: {
    type: Boolean,
    default: false,
  },
});

defineEmits(['close', 'update-backend']);

const { t } = useI18n({ useScope: 'global' });
</script>
