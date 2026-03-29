<template>
  <div class="container py-8 sm:py-12 px-4">
    <div class="mx-auto max-w-7xl space-y-6">
      <section
        class="rounded-2xl border border-cyan-900/40 bg-gray-800/80 p-6 shadow-2xl"
      >
        <div
          class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between"
        >
          <div>
            <h1 class="text-3xl font-bold text-white">{{ tr("title") }}</h1>
          </div>
          <div
            class="flex flex-wrap items-center justify-end gap-2 self-start lg:ml-auto"
          >
            <button
              type="button"
              :title="tr('buttons.automation')"
              :aria-label="tr('buttons.automation')"
              class="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
              @click="openSettingsModal('automation')"
            >
              <Cog6ToothIcon class="h-5 w-5" />
            </button>
            <button
              type="button"
              :title="tr('buttons.camera')"
              :aria-label="tr('buttons.camera')"
              class="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
              @click="openSettingsModal('camera')"
            >
              <CameraIcon class="h-5 w-5" />
            </button>
            <button
              type="button"
              :title="tr('buttons.outputs')"
              :aria-label="tr('buttons.outputs')"
              class="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
              @click="openSettingsModal('outputs')"
            >
              <PhotoIcon class="h-5 w-5" />
            </button>
            <button
              type="button"
              :title="tr('buttons.showStatus')"
              :aria-label="tr('buttons.showStatus')"
              class="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
              @click="showStatusModal = true"
            >
              <InformationCircleIcon class="h-5 w-5" />
            </button>
          </div>
        </div>
      </section>

      <div
        v-if="showStatusModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4 py-6 backdrop-blur-sm"
        @click.self="showStatusModal = false"
      >
        <div
          class="w-full max-w-2xl rounded-2xl border border-gray-700 bg-gray-900/95 p-6 shadow-2xl"
        >
          <div class="flex items-start justify-between gap-4">
            <h2 class="text-xl font-semibold text-white">
              {{ tr("statusModal.title") }}
            </h2>
            <button
              type="button"
              class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-gray-600 bg-gray-800/80 text-gray-200 transition hover:border-cyan-400 hover:text-white"
              :aria-label="tr('statusModal.close')"
              @click="showStatusModal = false"
            >
              <XMarkIcon class="h-5 w-5" />
            </button>
          </div>

          <div
            class="mt-6 grid grid-cols-[auto_minmax(0,1fr)] gap-x-8 gap-y-3 lg:grid-cols-[auto_minmax(0,1fr)_auto_minmax(0,1fr)]"
          >
            <template v-for="item in statusRows" :key="item.label">
              <div
                class="text-xs font-semibold uppercase tracking-wide text-gray-500"
              >
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
              :disabled="
                backendUpdateBusy ||
                status?.captureRunning ||
                status?.generateInProgress
              "
              :title="tr('statusModal.updateBackendTooltip')"
              @click="updateBackend"
            >
              {{
                backendUpdateBusy
                  ? tr("buttons.startingUpdate")
                  : tr("buttons.updateBackend")
              }}
            </button>
            <button
              type="button"
              class="rounded-xl bg-cyan-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-cyan-500"
              @click="showStatusModal = false"
            >
              {{ tr("common.ok") }}
            </button>
          </div>
        </div>
      </div>

      <div
        v-if="showBackendSetupBanner"
        class="rounded-xl border border-cyan-500/30 bg-cyan-500/10 px-4 py-4 text-sm text-cyan-100"
      >
        <div class="font-semibold text-white">
          {{ tr("banners.backendUnavailableTitle") }}
        </div>
        <p class="mt-1">
          {{ tr("banners.backendUnavailableBody") }}
        </p>
        <div
          class="mt-3 text-xs font-semibold uppercase tracking-wide text-cyan-200/80"
        >
          {{ tr("banners.backendUnavailableInstallLabel") }}
        </div>
        <pre
          class="mt-2 overflow-x-auto rounded-xl border border-cyan-500/20 bg-gray-950/70 px-4 py-3 text-xs text-cyan-50"
        ><code>{{ backendInstallCommands }}</code></pre>
      </div>

      <div
        v-else-if="error"
        class="rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-100"
      >
        {{ error }}
      </div>

      <div
        v-if="backendError"
        class="rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-100"
      >
        {{ backendError }}
      </div>

      <div
        v-if="actionMessage"
        class="rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-100"
      >
        {{ actionMessage }}
      </div>

      <div
        v-if="missingDependencies.length > 0"
        class="rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-100"
      >
        {{
          tr("banners.missingRuntimeDependencies", {
            dependencies: missingDependencies.join(", "),
          })
        }}
      </div>

      <section
        class="rounded-2xl border border-gray-700 bg-gray-800/80 p-5 shadow-xl"
      >
        <h2 class="text-xl font-semibold text-white">
          {{ tr("sections.sessionControl") }}
        </h2>

        <div class="mt-5 space-y-5">
          <div class="flex flex-wrap items-stretch gap-2">
            <input
              v-model="manualLabelInput"
              :title="
                tr('estimate.sessionLabelTitle', { label: defaultManualLabel })
              "
              class="min-w-0 flex-1 rounded-xl border border-gray-600 bg-gray-900/80 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              :placeholder="tr('estimate.sessionLabelPlaceholder')"
            />

            <div class="flex items-center gap-2">
              <button
                type="button"
                :title="tr('buttons.startCapture')"
                :aria-label="tr('buttons.startCapture')"
                class="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-600 text-white transition hover:bg-emerald-500 disabled:cursor-not-allowed disabled:opacity-40"
                :disabled="loading || status?.captureRunning"
                @click="startSession"
              >
                <PlayIcon class="h-5 w-5" />
              </button>
              <button
                type="button"
                :title="
                  status?.generateInProgress
                    ? tr('buttons.renderingProgress')
                    : tr('buttons.renderProgress')
                "
                :aria-label="
                  status?.generateInProgress
                    ? tr('buttons.renderingProgress')
                    : tr('buttons.renderProgress')
                "
                class="inline-flex h-11 w-11 items-center justify-center rounded-xl border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                :disabled="
                  loading ||
                  !status?.captureRunning ||
                  !currentSession?.captureCount ||
                  status?.generateInProgress
                "
                @click="generateArtifacts(currentSession?.id || null)"
              >
                <ArrowDownTrayIcon
                  class="h-5 w-5"
                  :class="status?.generateInProgress ? 'animate-pulse' : ''"
                />
              </button>
              <button
                type="button"
                :title="tr('buttons.stopAndRender')"
                :aria-label="tr('buttons.stopAndRender')"
                class="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-rose-600 text-white transition hover:bg-rose-500 disabled:cursor-not-allowed disabled:opacity-40"
                :disabled="loading || !status?.captureRunning"
                @click="stopSession"
              >
                <StopIcon class="h-5 w-5" />
              </button>
            </div>
          </div>

          <div
            class="overflow-hidden rounded-2xl border border-gray-700 bg-black/60"
          >
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
                {{ tr("preview.noFrameYet") }}
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
                  {{ tr("sections.livePreview") }}
                </div>
                <button
                  type="button"
                  :title="
                    loading
                      ? tr('buttons.refreshingPreview')
                      : tr('buttons.refreshPreview')
                  "
                  :aria-label="
                    loading
                      ? tr('buttons.refreshingPreview')
                      : tr('buttons.refreshPreview')
                  "
                  class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-white/10 bg-black/45 text-gray-200 backdrop-blur-sm transition hover:border-cyan-400 hover:text-white disabled:cursor-not-allowed disabled:opacity-40"
                  :disabled="loading"
                  @click="refreshAll"
                >
                  <ArrowPathIcon
                    class="h-5 w-5"
                    :class="loading ? 'animate-spin' : ''"
                  />
                </button>
              </div>

              <div
                class="pointer-events-none absolute left-4 top-4 rounded-xl border border-white/10 bg-black/45 px-3 py-2 backdrop-blur-sm"
              >
                <div
                  class="text-[11px] font-semibold uppercase tracking-[0.2em] text-gray-300"
                >
                  {{ tr("preview.session") }}
                </div>
                <div class="mt-1 text-sm font-medium text-white">
                  {{ currentSession?.id || tr("preview.noActiveSession") }}
                </div>
              </div>

              <div
                class="pointer-events-none absolute bottom-4 left-4 rounded-xl border border-white/10 bg-black/45 px-3 py-2 backdrop-blur-sm"
              >
                <div class="space-y-1 text-sm text-white">
                  <div>
                    <span class="text-gray-300">{{
                      tr("preview.lastCapture")
                    }}</span>
                    {{ formatDate(currentSession?.lastCaptureAtUtc) }}
                  </div>
                  <div>
                    <span class="text-gray-300">{{
                      tr("preview.frameCount")
                    }}</span>
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
                      tr("preview.captureInterval")
                    }}</span>
                    {{ formatInterval(config?.camera?.intervalSeconds) }}
                  </div>
                  <div>
                    <span class="text-gray-300">{{
                      tr("preview.exposure")
                    }}</span>
                    {{ formatExposure(config?.camera) }}
                  </div>
                  <div>
                    <span class="text-gray-300">{{ tr("preview.gain") }}</span>
                    {{ formatGain(config?.camera) }}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="rounded-2xl border border-gray-700 bg-gray-900/50 p-4">
            <div class="text-sm font-semibold text-white">
              {{ tr("sections.sessionEstimate") }}
            </div>
            <div class="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
              <label
                class="block rounded-xl border border-gray-700 bg-gray-900/60 p-3"
              >
                <span
                  class="block text-xs uppercase tracking-wide text-gray-500"
                  >{{ tr("estimate.start") }}</span
                >
                <input
                  v-model="estimateWindow.startLocal"
                  type="datetime-local"
                  :title="tr('estimate.startTooltip')"
                  class="mt-2 w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label
                class="block rounded-xl border border-gray-700 bg-gray-900/60 p-3"
              >
                <span
                  class="block text-xs uppercase tracking-wide text-gray-500"
                  >{{ tr("estimate.end") }}</span
                >
                <input
                  v-model="estimateWindow.endLocal"
                  type="datetime-local"
                  :title="tr('estimate.endTooltip')"
                  class="mt-2 w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
                <div class="text-xs uppercase tracking-wide text-gray-500">
                  {{ tr("estimate.expectedDuration") }}
                </div>
                <div class="mt-2 text-sm text-white">
                  {{ estimateDurationLabel }}
                </div>
              </div>
              <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
                <div class="text-xs uppercase tracking-wide text-gray-500">
                  {{ tr("estimate.expectedFrames") }}
                </div>
                <div class="mt-2 text-sm text-white">
                  {{ formatCount(estimatedFrameCount) }}
                </div>
              </div>
              <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
                <div class="text-xs uppercase tracking-wide text-gray-500">
                  {{ tr("estimate.expectedStorage") }}
                </div>
                <div
                  class="mt-2 text-sm font-semibold"
                  :class="
                    estimateExceedsAvailable ? 'text-amber-300' : 'text-white'
                  "
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

      <div
        v-if="settingsModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4 py-6 backdrop-blur-sm"
        @click.self="closeSettingsModal()"
      >
        <div
          class="w-full max-w-6xl max-h-[90vh] overflow-y-auto rounded-2xl border border-gray-700 bg-gray-900/95 p-6 shadow-2xl"
        >
          <div class="flex items-start justify-between gap-4">
            <h2 class="text-xl font-semibold text-white">
              {{ settingsModalTitle }}
            </h2>
            <button
              type="button"
              class="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-gray-600 bg-gray-800/80 text-gray-200 transition hover:border-cyan-400 hover:text-white"
              :disabled="saving"
              :aria-label="
                tr('modals.closeDialog', { title: settingsModalTitle })
              "
              @click="closeSettingsModal()"
            >
              <XMarkIcon class="h-5 w-5" />
            </button>
          </div>

          <div
            v-if="settingsModal === 'automation'"
            class="mt-6 flex flex-wrap gap-4"
          >
            <label
              :class="settingsFieldClass"
              class="rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2"
            >
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.automation.autoStartWithSequence") }}
              </span>
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm text-gray-300">{{
                  tr("modals.automation.sequenceTriggeredCapture")
                }}</span>
                <toggleButton
                  :status-value="Boolean(config.autoStartWithSequence)"
                  @update:statusValue="
                    setRootConfigValue('autoStartWithSequence', $event)
                  "
                />
              </div>
            </label>

            <div
              :class="settingsWideFieldClass"
              class="rounded-xl border border-cyan-500/20 bg-cyan-500/10 px-3 py-2 text-sm text-cyan-100"
            >
              {{ tr("modals.automationInfo") }}
            </div>

            <label
              :class="settingsFieldClass"
              class="rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2"
            >
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.automation.advancedApiEnabled") }}
              </span>
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm text-gray-300">{{
                  tr("modals.automation.backendMonitoring")
                }}</span>
                <toggleButton
                  :status-value="Boolean(config.advancedApi.enabled)"
                  @update:statusValue="setAdvancedApiSetting('enabled', $event)"
                />
              </div>
            </label>

            <label :class="settingsFieldClass" class="block">
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.automation.protocol") }}
              </span>
              <input
                v-model="config.advancedApi.protocol"
                :title="tr('modals.automation.protocolTooltip')"
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
            </label>

            <label :class="settingsFieldClass" class="block">
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.automation.sequencePollIntervalSeconds") }}
              </span>
              <input
                v-model.number="config.sequencePollIntervalSeconds"
                type="number"
                min="5"
                max="300"
                :title="tr('modals.automation.sequencePollTooltip')"
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
              <span class="mt-2 block text-xs text-gray-500">
                {{ tr("modals.automation.sequencePollIntervalHelp") }}
              </span>
            </label>

            <label :class="settingsFieldClass" class="block">
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.automation.advancedApiHost") }}
              </span>
              <input
                v-model="config.advancedApi.host"
                :title="tr('modals.automation.advancedApiHostTooltip')"
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
            </label>

            <label :class="settingsFieldClass" class="block">
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.automation.port") }}
              </span>
              <input
                v-model.number="config.advancedApi.port"
                type="number"
                :title="tr('modals.automation.portTooltip')"
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
            </label>

            <label :class="settingsFieldClass" class="block">
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.automation.timeoutSeconds") }}
              </span>
              <input
                v-model.number="config.advancedApi.requestTimeoutSeconds"
                type="number"
                min="1"
                max="30"
                :title="tr('modals.automation.timeoutTooltip')"
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
            </label>

            <label :class="settingsFieldClass" class="block">
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.automation.advancedApiBasePath") }}
              </span>
              <input
                v-model="config.advancedApi.basePath"
                :title="tr('modals.automation.advancedApiBasePathTooltip')"
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
            </label>

            <label :class="settingsFieldClass" class="block">
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.automation.maxPluginStorageGb") }}
              </span>
              <input
                v-model.number="config.storage.maxUsageGb"
                type="number"
                min="0"
                step="0.1"
                inputmode="decimal"
                :title="tr('modals.automation.maxPluginStorageTooltip')"
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
              <span class="mt-2 block text-xs text-gray-500">
                {{ tr("modals.automation.maxPluginStorageHelp") }}
              </span>
            </label>
          </div>

          <div v-else-if="settingsModal === 'camera'" class="mt-6 space-y-4">
            <div class="flex flex-wrap gap-4">
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.intervalSeconds") }}
                </span>
                <input
                  v-model.number="config.camera.intervalSeconds"
                  type="number"
                  min="5"
                  :title="tr('modals.camera.intervalTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.timeoutSeconds") }}
                </span>
                <input
                  v-model.number="config.camera.captureTimeoutSeconds"
                  type="number"
                  min="15"
                  :title="tr('modals.camera.timeoutTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.width") }}
                </span>
                <input
                  v-model.number="config.camera.width"
                  type="number"
                  min="640"
                  :title="tr('modals.camera.widthTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.height") }}
                </span>
                <input
                  v-model.number="config.camera.height"
                  type="number"
                  min="480"
                  :title="tr('modals.camera.heightTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.jpegQuality") }}
                </span>
                <input
                  v-model.number="config.camera.quality"
                  type="number"
                  min="1"
                  max="100"
                  :title="tr('modals.camera.jpegQualityTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.warmupMilliseconds") }}
                </span>
                <input
                  v-model.number="config.camera.warmupMilliseconds"
                  type="number"
                  min="1"
                  :title="tr('modals.camera.warmupTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.metering") }}
                </span>
                <select
                  v-model="config.camera.meteringMode"
                  :title="tr('modals.camera.meteringTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                >
                  <option
                    v-for="option in meteringOptions"
                    :key="option.value"
                    :value="option.value"
                  >
                    {{ option.label }}
                  </option>
                </select>
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.awb") }}
                </span>
                <select
                  v-model="config.camera.awbMode"
                  :title="tr('modals.camera.awbTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                >
                  <option
                    v-for="option in awbOptions"
                    :key="option.value"
                    :value="option.value"
                  >
                    {{ option.label }}
                  </option>
                </select>
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.denoise") }}
                </span>
                <select
                  v-model="config.camera.denoiseMode"
                  :title="tr('modals.camera.denoiseTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                >
                  <option
                    v-for="option in denoiseOptions"
                    :key="option.value"
                    :value="option.value"
                  >
                    {{ option.label }}
                  </option>
                </select>
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.evCompensation") }}
                </span>
                <input
                  v-model.number="config.camera.evCompensation"
                  type="number"
                  step="0.1"
                  inputmode="decimal"
                  :title="tr('modals.camera.evCompensationTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.rotation") }}
                </span>
                <input
                  v-model.number="config.camera.rotation"
                  type="number"
                  min="0"
                  max="180"
                  :title="tr('modals.camera.rotationTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.brightness") }}
                </span>
                <input
                  v-model.number="config.camera.brightness"
                  type="number"
                  step="0.1"
                  inputmode="decimal"
                  :title="tr('modals.camera.brightnessTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.contrast") }}
                </span>
                <input
                  v-model.number="config.camera.contrast"
                  type="number"
                  min="0"
                  step="0.1"
                  inputmode="decimal"
                  :title="tr('modals.camera.contrastTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.saturation") }}
                </span>
                <input
                  v-model.number="config.camera.saturation"
                  type="number"
                  min="0"
                  step="0.1"
                  inputmode="decimal"
                  :title="tr('modals.camera.saturationTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label :class="settingsFieldClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.sharpness") }}
                </span>
                <input
                  v-model.number="config.camera.sharpness"
                  type="number"
                  min="0"
                  step="0.1"
                  inputmode="decimal"
                  :title="tr('modals.camera.sharpnessTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
            </div>

            <div class="flex flex-wrap gap-4">
              <div
                class="w-full lg:w-[calc(50%-0.5rem)] rounded-xl border border-gray-700 bg-gray-800/70 p-3"
              >
                <label class="flex items-center justify-between gap-3">
                  <span class="text-sm text-gray-300">{{
                    tr("modals.camera.manualExposure")
                  }}</span>
                  <toggleButton
                    :status-value="Boolean(config.camera.useManualExposure)"
                    @update:statusValue="
                      setCameraSetting('useManualExposure', $event)
                    "
                  />
                </label>
                <label class="mt-3 block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.camera.shutterMicroseconds") }}
                  </span>
                  <input
                    v-model.number="config.camera.shutterMicroseconds"
                    type="number"
                    min="1"
                    inputmode="numeric"
                    :disabled="!config.camera.useManualExposure"
                    :title="tr('modals.camera.shutterTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400 disabled:cursor-not-allowed disabled:opacity-50"
                  />
                </label>
                <p class="mt-3 text-xs text-gray-500">
                  {{ tr("modals.camera.manualExposureHelp") }}
                </p>
              </div>

              <div
                class="w-full lg:w-[calc(50%-0.5rem)] rounded-xl border border-gray-700 bg-gray-800/70 p-3"
              >
                <label class="flex items-center justify-between gap-3">
                  <span class="text-sm text-gray-300">{{
                    tr("modals.camera.manualGain")
                  }}</span>
                  <toggleButton
                    :status-value="Boolean(config.camera.useManualGain)"
                    @update:statusValue="
                      setCameraSetting('useManualGain', $event)
                    "
                  />
                </label>
                <label class="mt-3 block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.camera.analogGain") }}
                  </span>
                  <input
                    v-model.number="config.camera.analogGain"
                    type="number"
                    min="1"
                    step="0.1"
                    inputmode="decimal"
                    :disabled="!config.camera.useManualGain"
                    :title="tr('modals.camera.analogGainTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400 disabled:cursor-not-allowed disabled:opacity-50"
                  />
                </label>
                <p class="mt-3 text-xs text-gray-500">
                  {{ tr("modals.camera.manualGainHelp") }}
                </p>
              </div>
            </div>

            <div class="flex flex-wrap gap-4">
              <div
                :class="settingsFullWidthClass"
                class="rounded-xl border border-cyan-500/20 bg-cyan-500/10 px-3 py-2 text-sm text-cyan-100"
              >
                {{ tr("modals.cameraRotationInfo") }}
              </div>
              <label
                :class="settingsFieldClass"
                class="rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2"
              >
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.horizontalFlip") }}
                </span>
                <div class="flex items-center justify-between gap-3">
                  <span class="text-sm text-gray-300">{{
                    tr("modals.camera.mirrorOnXAxis")
                  }}</span>
                  <toggleButton
                    :status-value="Boolean(config.camera.horizontalFlip)"
                    @update:statusValue="
                      setCameraSetting('horizontalFlip', $event)
                    "
                  />
                </div>
              </label>
              <label
                :class="settingsFieldClass"
                class="rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2"
              >
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.verticalFlip") }}
                </span>
                <div class="flex items-center justify-between gap-3">
                  <span class="text-sm text-gray-300">{{
                    tr("modals.camera.mirrorOnYAxis")
                  }}</span>
                  <toggleButton
                    :status-value="Boolean(config.camera.verticalFlip)"
                    @update:statusValue="
                      setCameraSetting('verticalFlip', $event)
                    "
                  />
                </div>
              </label>
              <label :class="settingsFullWidthClass" class="block">
                <span
                  class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                >
                  {{ tr("modals.camera.extraArguments") }}
                </span>
                <textarea
                  v-model="config.camera.extraArguments"
                  rows="3"
                  :title="tr('modals.camera.extraArgumentsTooltip')"
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
            </div>
          </div>

          <div v-else class="mt-6 space-y-4">
            <label
              class="block rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2"
            >
              <span
                class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
              >
                {{ tr("modals.outputs.keepSourceFrames") }}
              </span>
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm text-gray-300">{{
                  tr("modals.outputs.retainOriginalCaptures")
                }}</span>
                <toggleButton
                  :status-value="Boolean(config.products.keepFrames)"
                  @update:statusValue="setProductSetting('keepFrames', $event)"
                />
              </div>
            </label>

            <div class="rounded-xl border border-gray-700 bg-gray-800/70 p-4">
              <div class="flex items-center justify-between gap-3">
                <span class="font-semibold text-white">{{
                  tr("modals.outputs.timelapse")
                }}</span>
                <toggleButton
                  :status-value="Boolean(config.products.timelapseEnabled)"
                  @update:statusValue="
                    setProductSetting('timelapseEnabled', $event)
                  "
                />
              </div>
              <div
                v-if="config.products.timelapseEnabled"
                class="mt-4 flex flex-wrap gap-4"
              >
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.fps") }}
                  </span>
                  <input
                    v-model.number="config.products.timelapseFps"
                    type="number"
                    min="1"
                    max="60"
                    :title="tr('modals.outputs.fpsTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.bitrateKbps") }}
                  </span>
                  <input
                    v-model.number="config.products.timelapseBitrateKbps"
                    type="number"
                    min="1000"
                    :title="tr('modals.outputs.bitrateTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.width") }}
                  </span>
                  <input
                    v-model.number="config.products.timelapseWidth"
                    type="number"
                    min="320"
                    :title="tr('modals.outputs.timelapseWidthTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.height") }}
                  </span>
                  <input
                    v-model.number="config.products.timelapseHeight"
                    type="number"
                    min="240"
                    :title="tr('modals.outputs.timelapseHeightTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.codec") }}
                  </span>
                  <input
                    v-model="config.products.timelapseCodec"
                    :title="tr('modals.outputs.codecTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.pixelFormat") }}
                  </span>
                  <input
                    v-model="config.products.timelapsePixelFormat"
                    :title="tr('modals.outputs.pixelFormatTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.ffmpegLogLevel") }}
                  </span>
                  <input
                    v-model="config.products.timelapseLogLevel"
                    :title="tr('modals.outputs.ffmpegLogLevelTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFullWidthClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.extraFfmpegArguments") }}
                  </span>
                  <textarea
                    v-model="config.products.timelapseExtraParameters"
                    rows="3"
                    :title="tr('modals.outputs.extraFfmpegArgumentsTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
              </div>
            </div>

            <div class="rounded-xl border border-gray-700 bg-gray-800/70 p-4">
              <div class="flex items-center justify-between gap-3">
                <span class="font-semibold text-white">{{
                  tr("modals.outputs.keogram")
                }}</span>
                <toggleButton
                  :status-value="Boolean(config.products.keogramEnabled)"
                  @update:statusValue="
                    setProductSetting('keogramEnabled', $event)
                  "
                />
              </div>
              <div
                v-if="config.products.keogramEnabled"
                class="mt-4 flex flex-wrap gap-4"
              >
                <label
                  :class="settingsFieldClass"
                  class="rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2"
                >
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.expandToFrameWidth") }}
                  </span>
                  <div class="flex items-center justify-between gap-3">
                    <span class="text-sm text-gray-300">{{
                      tr("modals.outputs.stretchOutput")
                    }}</span>
                    <toggleButton
                      :status-value="Boolean(config.products.keogramExpand)"
                      @update:statusValue="
                        setProductSetting('keogramExpand', $event)
                      "
                    />
                  </div>
                </label>
                <label
                  :class="settingsFieldClass"
                  class="rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2"
                >
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.showLabels") }}
                  </span>
                  <div class="flex items-center justify-between gap-3">
                    <span class="text-sm text-gray-300">{{
                      tr("modals.outputs.drawCaptions")
                    }}</span>
                    <toggleButton
                      :status-value="Boolean(config.products.keogramShowLabels)"
                      @update:statusValue="
                        setProductSetting('keogramShowLabels', $event)
                      "
                    />
                  </div>
                </label>
                <label
                  :class="settingsFieldClass"
                  class="rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2"
                >
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.showDate") }}
                  </span>
                  <div class="flex items-center justify-between gap-3">
                    <span class="text-sm text-gray-300">{{
                      tr("modals.outputs.stampDate")
                    }}</span>
                    <toggleButton
                      :status-value="Boolean(config.products.keogramShowDate)"
                      @update:statusValue="
                        setProductSetting('keogramShowDate', $event)
                      "
                    />
                  </div>
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.rotateDegrees") }}
                  </span>
                  <input
                    v-model.number="config.products.keogramRotateDegrees"
                    type="number"
                    inputmode="decimal"
                    :title="tr('modals.outputs.rotateTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.fontName") }}
                  </span>
                  <input
                    v-model="config.products.keogramFontName"
                    :title="tr('modals.outputs.fontNameTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.fontColor") }}
                  </span>
                  <input
                    v-model="config.products.keogramFontColor"
                    :title="tr('modals.outputs.fontColorTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.fontSize") }}
                  </span>
                  <input
                    v-model.number="config.products.keogramFontSize"
                    type="number"
                    min="0.1"
                    step="0.1"
                    inputmode="decimal"
                    :title="tr('modals.outputs.fontSizeTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.lineThickness") }}
                  </span>
                  <input
                    v-model.number="config.products.keogramLineThickness"
                    type="number"
                    min="1"
                    :title="tr('modals.outputs.lineThicknessTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label :class="settingsFullWidthClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.extraKeogramArguments") }}
                  </span>
                  <textarea
                    v-model="config.products.keogramExtraParameters"
                    rows="3"
                    :title="tr('modals.outputs.extraKeogramArgumentsTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
              </div>
            </div>

            <div class="rounded-xl border border-gray-700 bg-gray-800/70 p-4">
              <div class="flex items-center justify-between gap-3">
                <span class="font-semibold text-white">{{
                  tr("modals.outputs.startrails")
                }}</span>
                <toggleButton
                  :status-value="Boolean(config.products.startrailsEnabled)"
                  @update:statusValue="
                    setProductSetting('startrailsEnabled', $event)
                  "
                />
              </div>
              <div
                v-if="config.products.startrailsEnabled"
                class="mt-4 flex flex-wrap gap-4"
              >
                <label :class="settingsFieldClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.brightnessThreshold") }}
                  </span>
                  <input
                    v-model.number="
                      config.products.startrailsBrightnessThreshold
                    "
                    type="number"
                    min="0"
                    max="1"
                    step="0.01"
                    inputmode="decimal"
                    :title="tr('modals.outputs.brightnessThresholdTooltip')"
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <div
                  :class="settingsWideFieldClass"
                  class="rounded-xl border border-amber-500/20 bg-amber-500/10 px-3 py-2 text-sm text-amber-100"
                >
                  {{ tr("modals.startrailMountInfo") }}
                </div>
                <label :class="settingsFullWidthClass" class="block">
                  <span
                    class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400"
                  >
                    {{ tr("modals.outputs.extraStartrailsArguments") }}
                  </span>
                  <textarea
                    v-model="config.products.startrailsExtraParameters"
                    rows="3"
                    :title="
                      tr('modals.outputs.extraStartrailsArgumentsTooltip')
                    "
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/60 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
              </div>
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3">
            <button
              type="button"
              class="rounded-xl border border-gray-600 bg-gray-800/80 px-4 py-2 text-sm font-semibold text-gray-200 transition hover:border-cyan-400 hover:text-white"
              :disabled="saving"
              @click="closeSettingsModal()"
            >
              {{ tr("common.cancel") }}
            </button>
            <button
              type="button"
              class="rounded-xl bg-cyan-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-cyan-500 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="saving"
              @click="saveSettingsModal"
            >
              {{ saving ? tr("common.saving") : tr("common.ok") }}
            </button>
          </div>
        </div>
      </div>

      <section
        class="rounded-2xl border border-gray-700 bg-gray-800/80 p-6 shadow-xl"
      >
        <button
          type="button"
          class="flex w-full items-start justify-between gap-4 text-left"
          @click="toggleSection('recentSessions')"
        >
          <div>
            <h2 class="text-xl font-semibold text-white">
              {{ tr("sections.recentSessions") }}
            </h2>
            <p class="mt-1 text-sm text-gray-400">
              {{ tr("sections.recentSessionsDescription") }}
            </p>
          </div>
          <div class="flex items-center gap-3">
            <span
              class="rounded-full border border-gray-600 bg-gray-900/70 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-gray-300"
            >
              {{
                sectionOpen.recentSessions
                  ? tr("recentSessions.expanded")
                  : tr("recentSessions.collapsed")
              }}
            </span>
            <span
              class="flex h-9 w-9 items-center justify-center rounded-full border border-cyan-500/40 bg-cyan-500/10 text-lg font-semibold text-cyan-200"
            >
              {{ sectionOpen.recentSessions ? "-" : "+" }}
            </span>
          </div>
        </button>

        <div v-if="sectionOpen.recentSessions" class="mt-6 space-y-4">
          <div
            class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-900/50 px-4 py-3"
          >
            <div class="text-sm text-gray-300">
              <span
                :class="
                  storage.withinLimit ? 'text-emerald-300' : 'text-amber-200'
                "
              >
                {{
                  storage.withinLimit || !storage.limitEnabled
                    ? tr("recentSessions.storageWithinLimit")
                    : tr("recentSessions.storageLimitExceeded")
                }}
              </span>
              <span class="text-gray-500">
                {{ tr("recentSessions.pluginAvailable") }}
                {{
                  storage.limitEnabled
                    ? formatSize(storage.pluginAvailableBytes)
                    : tr("common.unlimited")
                }}.
              </span>
              <span class="text-gray-500">
                {{ tr("recentSessions.piAvailable") }}
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
              :title="tr('buttons.deleteAllSessions')"
              :aria-label="tr('buttons.deleteAllSessions')"
              @click.stop="deleteAllSessions"
            >
              <TrashIcon class="h-5 w-5" />
            </button>
          </div>
        </div>

        <div
          v-if="sectionOpen.recentSessions && recentSessions.length === 0"
          class="mt-6 rounded-xl border border-dashed border-gray-700 bg-gray-900/40 px-4 py-8 text-center text-sm text-gray-500"
        >
          {{ tr("recentSessions.noSessions") }}
        </div>

        <div v-else-if="sectionOpen.recentSessions" class="mt-6 space-y-4">
          <article
            v-for="session in recentSessions"
            :key="session.id"
            class="rounded-2xl border border-gray-700 bg-gray-900/50 p-4"
          >
            <div
              class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between"
            >
              <div class="space-y-1">
                <div class="text-lg font-semibold text-white">
                  {{ session.label || session.id }}
                </div>
                <div class="text-sm text-gray-400">
                  {{
                    tr("recentSessions.started", {
                      date: formatDate(session.startedAtUtc),
                    })
                  }}
                  <span v-if="session.endedAtUtc">
                    •
                    {{
                      tr("recentSessions.stopped", {
                        date: formatDate(session.endedAtUtc),
                      })
                    }}</span
                  >
                </div>
                <div class="text-sm text-gray-400">
                  {{
                    tr("recentSessions.framesReason", {
                      count: formatCount(session.captureCount),
                      reason: formatSessionReason(session.startReason),
                    })
                  }}
                  <span v-if="session.stopReason">
                    • {{ formatSessionReason(session.stopReason) }}</span
                  >
                </div>
                <div class="text-sm text-gray-400">
                  {{
                    tr("recentSessions.storage", {
                      size: formatSize(session.totalSizeBytes),
                    })
                  }}
                </div>
              </div>

              <div class="flex flex-wrap gap-2">
                <button
                  class="rounded-lg border border-cyan-500/40 bg-cyan-500/10 px-3 py-2 text-sm font-medium text-cyan-100 transition hover:bg-cyan-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                  :disabled="cleanupBusy"
                  @click="generateArtifacts(session.id)"
                >
                  {{ tr("buttons.regenerate") }}
                </button>
                <button
                  class="inline-flex h-10 w-10 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                  :disabled="
                    cleanupBusy ||
                    status?.captureRunning ||
                    status?.generateInProgress ||
                    session.id === currentSession?.id
                  "
                  :title="tr('buttons.deleteSession')"
                  :aria-label="tr('buttons.deleteSession')"
                  @click="deleteSession(session)"
                >
                  <TrashIcon class="h-5 w-5" />
                </button>
                <button
                  class="inline-flex h-10 items-center justify-center gap-2 rounded-lg border border-gray-600 bg-gray-800 px-3 py-2 text-sm text-white transition hover:border-cyan-400"
                  :title="
                    isSessionDetailsOpen(session.id)
                      ? tr('buttons.hideStoredFiles')
                      : tr('buttons.showStoredFiles')
                  "
                  :aria-label="
                    isSessionDetailsOpen(session.id)
                      ? tr('buttons.hideStoredFiles')
                      : tr('buttons.showStoredFiles')
                  "
                  @click="toggleSessionDetails(session.id)"
                >
                  <span>{{ tr("buttons.files") }}</span>
                  <ChevronUpIcon
                    v-if="isSessionDetailsOpen(session.id)"
                    class="h-4 w-4"
                  />
                  <ChevronDownIcon v-else class="h-4 w-4" />
                </button>
              </div>
            </div>

            <div class="mt-4 grid gap-3 sm:grid-cols-3">
              <div
                class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300"
              >
                <div class="text-xs uppercase tracking-wide text-gray-500">
                  {{ tr("recentSessions.timelapse") }}
                </div>
                <div class="mt-2 flex items-start justify-between gap-3">
                  <div>{{ describeArtifact(session.products?.timelapse) }}</div>
                  <div
                    v-if="session.products?.timelapse"
                    class="flex items-center gap-2"
                  >
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
                      :title="tr('buttons.downloadTimelapse')"
                      :aria-label="tr('buttons.downloadTimelapse')"
                      @click="
                        downloadRelativePath(
                          session.products.timelapse.relativePath,
                        )
                      "
                    >
                      <ArrowDownTrayIcon class="h-4 w-4" />
                    </button>
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                      :disabled="
                        cleanupBusy ||
                        status?.generateInProgress ||
                        session.id === currentSession?.id
                      "
                      :title="tr('buttons.deleteTimelapse')"
                      :aria-label="tr('buttons.deleteTimelapse')"
                      @click="
                        deleteArtifact(session, session.products.timelapse)
                      "
                    >
                      <TrashIcon class="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
              <div
                class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300"
              >
                <div class="text-xs uppercase tracking-wide text-gray-500">
                  {{ tr("recentSessions.keogram") }}
                </div>
                <div class="mt-2 flex items-start justify-between gap-3">
                  <div>{{ describeArtifact(session.products?.keogram) }}</div>
                  <div
                    v-if="session.products?.keogram"
                    class="flex items-center gap-2"
                  >
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
                      :title="tr('buttons.downloadKeogram')"
                      :aria-label="tr('buttons.downloadKeogram')"
                      @click="
                        downloadRelativePath(
                          session.products.keogram.relativePath,
                        )
                      "
                    >
                      <ArrowDownTrayIcon class="h-4 w-4" />
                    </button>
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                      :disabled="
                        cleanupBusy ||
                        status?.generateInProgress ||
                        session.id === currentSession?.id
                      "
                      :title="tr('buttons.deleteKeogram')"
                      :aria-label="tr('buttons.deleteKeogram')"
                      @click="deleteArtifact(session, session.products.keogram)"
                    >
                      <TrashIcon class="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
              <div
                class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300"
              >
                <div class="text-xs uppercase tracking-wide text-gray-500">
                  {{ tr("recentSessions.startrails") }}
                </div>
                <div class="mt-2 flex items-start justify-between gap-3">
                  <div>
                    {{ describeArtifact(session.products?.startrails) }}
                  </div>
                  <div
                    v-if="session.products?.startrails"
                    class="flex items-center gap-2"
                  >
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
                      :title="tr('buttons.downloadStartrails')"
                      :aria-label="tr('buttons.downloadStartrails')"
                      @click="
                        downloadRelativePath(
                          session.products.startrails.relativePath,
                        )
                      "
                    >
                      <ArrowDownTrayIcon class="h-4 w-4" />
                    </button>
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                      :disabled="
                        cleanupBusy ||
                        status?.generateInProgress ||
                        session.id === currentSession?.id
                      "
                      :title="tr('buttons.deleteStartrails')"
                      :aria-label="tr('buttons.deleteStartrails')"
                      @click="
                        deleteArtifact(session, session.products.startrails)
                      "
                    >
                      <TrashIcon class="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <div
              v-if="isSessionDetailsOpen(session.id)"
              class="mt-4 rounded-2xl border border-gray-700 bg-gray-800/50 p-4"
            >
              <div class="mb-3 flex items-center justify-between gap-3">
                <div>
                  <div class="text-sm font-semibold text-white">
                    {{ tr("sections.storedFrames") }}
                  </div>
                  <div class="text-xs text-gray-400">
                    {{
                      tr("recentSessions.retainedFrames", {
                        count: formatCount(
                          sessionDetails(session.id)?.frames?.length ??
                            session.storedFrameCount ??
                            0,
                        ),
                      })
                    }}
                  </div>
                </div>
                <button
                  class="rounded-lg border border-gray-600 bg-gray-900/70 px-3 py-2 text-xs text-gray-200 transition hover:border-cyan-400"
                  @click="refreshSessionDetails(session.id)"
                >
                  {{ tr("buttons.refreshFiles") }}
                </button>
              </div>

              <div
                v-if="sessionDetailsLoading(session.id)"
                class="rounded-xl border border-dashed border-gray-700 bg-gray-900/40 px-4 py-6 text-center text-sm text-gray-500"
              >
                {{ tr("recentSessions.loadingStoredFiles") }}
              </div>
              <div
                v-else-if="!sessionDetails(session.id)?.frames?.length"
                class="rounded-xl border border-dashed border-gray-700 bg-gray-900/40 px-4 py-6 text-center text-sm text-gray-500"
              >
                {{ tr("recentSessions.noStoredFrames") }}
              </div>
              <div v-else class="max-h-80 space-y-2 overflow-y-auto pr-1">
                <div
                  v-for="frame in sessionDetails(session.id).frames"
                  :key="frame.relativePath"
                  class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2"
                >
                  <div class="min-w-0">
                    <div class="truncate text-sm text-white">
                      {{ frame.name }}
                    </div>
                    <div class="text-xs text-gray-400">
                      {{ formatDate(frame.capturedAtUtc) }} •
                      {{ formatSize(frame.sizeBytes) }}
                    </div>
                  </div>
                  <div class="flex items-center gap-2">
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
                      :title="tr('buttons.downloadItem', { name: frame.name })"
                      :aria-label="
                        tr('buttons.downloadItem', { name: frame.name })
                      "
                      @click="
                        downloadRelativePath(frame.relativePath, frame.name)
                      "
                    >
                      <ArrowDownTrayIcon class="h-4 w-4" />
                    </button>
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                      :disabled="
                        cleanupBusy ||
                        status?.generateInProgress ||
                        session.id === currentSession?.id
                      "
                      :title="tr('buttons.deleteItem', { name: frame.name })"
                      :aria-label="
                        tr('buttons.deleteItem', { name: frame.name })
                      "
                      @click="deleteFrame(session, frame)"
                    >
                      <TrashIcon class="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </article>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from "vue";
import { storeToRefs } from "pinia";
import { useI18n } from "vue-i18n";
import {
  ArrowDownTrayIcon,
  ArrowPathIcon,
  CameraIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  Cog6ToothIcon,
  InformationCircleIcon,
  PhotoIcon,
  PlayIcon,
  StopIcon,
  TrashIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";
import toggleButton from "@/components/helpers/toggleButton.vue";
import { usePinsAllSkyStore } from "../store/pinsAllskyStore";

const { t } = useI18n({ useScope: "global" });
const tr = (key, params) => t(`plugins.pinsAllSky.${key}`, params);
const store = usePinsAllSkyStore();
const {
  status,
  config,
  error,
  loading,
  saving,
  cleanupBusy,
  backendUpdateBusy,
  actionMessage,
  currentImageUrl,
  sessionDetailsById,
  detailsLoadingById,
} = storeToRefs(store);

const sectionOpen = reactive({
  recentSessions: false,
});

const sessionDetailOpen = reactive({});
const estimateWindow = reactive({
  startLocal: "",
  endLocal: "",
});
const showStatusModal = ref(false);
const settingsModal = ref(null);
const settingsModalSnapshot = ref(null);
const settingsFieldClass =
  "w-[calc(50%-0.5rem)] lg:w-[calc(33.333%-0.667rem)] 2xl:w-[calc(16.666%-0.834rem)]";
const settingsWideFieldClass =
  "w-full lg:w-[calc(66.666%-0.667rem)] 2xl:w-[calc(33.333%-0.667rem)]";
const settingsFullWidthClass = "w-full";

const currentSession = computed(() => status.value?.currentSession || null);
const recentSessions = computed(() => status.value?.recentSessions || []);
const backendError = computed(() => status.value?.lastError || null);
const storage = computed(() => {
  const raw = status.value?.storage || {};
  const diskAvailableBytes = Number(raw.diskAvailableBytes || 0);
  const diskTotalBytes = Number(raw.diskTotalBytes || 0);

  return {
    ...raw,
    diskUsedBytes: Number(
      raw.diskUsedBytes ?? Math.max(0, diskTotalBytes - diskAvailableBytes),
    ),
  };
});
const estimateBaseline = computed(() => status.value?.estimateBaseline || null);
const estimateBaselineAverageFrameBytes = computed(
  () => estimateBaseline.value?.averageFrameBytes || 0,
);
const showBackendSetupBanner = computed(
  () => !loading.value && !status.value && Boolean(error.value),
);
const backendInstallCommands = `cd /home/pi
git clone https://github.com/sharon92/pins-allsky-plugin.git
cd pins-allsky-plugin
./scripts/install-backend-plugin.sh --restart-pins`;

const dependencyRows = computed(() => [
  {
    label: tr("dependencies.rpiCamStill"),
    ready: Boolean(status.value?.dependencies?.rpiCamStillAvailable),
  },
  {
    label: tr("dependencies.ffmpeg"),
    ready: Boolean(status.value?.dependencies?.ffmpegAvailable),
  },
  {
    label: tr("dependencies.keogram"),
    ready: Boolean(status.value?.dependencies?.keogramAvailable),
  },
  {
    label: tr("dependencies.startrails"),
    ready: Boolean(status.value?.dependencies?.startrailsAvailable),
  },
]);

const missingDependencies = computed(() =>
  dependencyRows.value.filter((item) => !item.ready).map((item) => item.label),
);
const statusRows = computed(() => {
  const pluginLimitValue = storage.value.limitEnabled
    ? formatSize(storage.value.maxPluginUsageBytes)
    : tr("common.unlimited");
  const pluginAvailableValue = storage.value.limitEnabled
    ? formatSize(storage.value.pluginAvailableBytes)
    : tr("common.unlimited");

  return [
    {
      label: tr("statusRows.backend"),
      value: status.value?.advancedApiReachable
        ? tr("statusRows.online")
        : tr("statusRows.offline"),
      className: status.value?.advancedApiReachable
        ? "text-emerald-300"
        : "text-amber-300",
    },
    {
      label: tr("statusRows.session"),
      value:
        currentSession.value?.label ||
        currentSession.value?.id ||
        tr("preview.noActiveSession"),
      className: "text-white",
    },
    {
      label: tr("statusRows.sequence"),
      value: status.value?.sequenceRunning
        ? tr("statusRows.running")
        : tr("statusRows.idle"),
      className: status.value?.sequenceRunning
        ? "text-emerald-300"
        : "text-gray-300",
    },
    {
      label: tr("statusRows.capture"),
      value: status.value?.captureRunning
        ? tr("statusRows.active")
        : tr("statusRows.stopped"),
      className: status.value?.captureRunning
        ? "text-emerald-300"
        : "text-gray-300",
    },
    {
      label: tr("statusRows.rendering"),
      value: status.value?.generateInProgress
        ? tr("statusRows.inProgress")
        : tr("statusRows.idle"),
      className: status.value?.generateInProgress
        ? "text-cyan-300"
        : "text-gray-300",
    },
    {
      label: tr("statusRows.piUsed"),
      value: formatSize(storage.value.diskUsedBytes),
      className: "text-white",
    },
    {
      label: tr("statusRows.piAvailable"),
      value: formatSize(storage.value.diskAvailableBytes),
      className: estimateExceedsAvailable.value
        ? "text-amber-300"
        : "text-white",
    },
    {
      label: tr("statusRows.pluginUsed"),
      value: formatSize(storage.value.pluginUsedBytes),
      className: "text-white",
    },
    {
      label: tr("statusRows.pluginLimit"),
      value: pluginLimitValue,
      className: "text-white",
    },
    {
      label: tr("statusRows.pluginAvailable"),
      value: pluginAvailableValue,
      className:
        storage.value.withinLimit || !storage.value.limitEnabled
          ? "text-white"
          : "text-amber-300",
    },
    ...dependencyRows.value.map((item) => ({
      label: item.label,
      value: item.ready ? tr("statusRows.available") : tr("statusRows.missing"),
      className: item.ready ? "text-emerald-300" : "text-amber-300",
    })),
  ];
});

const parsedEstimateStart = computed(() =>
  parseLocalInputValue(estimateWindow.startLocal),
);
const parsedEstimateEnd = computed(() =>
  parseLocalInputValue(estimateWindow.endLocal),
);
const estimateDurationSeconds = computed(() => {
  if (!parsedEstimateStart.value || !parsedEstimateEnd.value) {
    return 0;
  }

  return Math.max(
    0,
    Math.round(
      (parsedEstimateEnd.value.getTime() -
        parsedEstimateStart.value.getTime()) /
        1000,
    ),
  );
});
const estimatedFrameCount = computed(() => {
  const intervalSeconds = Number(config.value?.camera?.intervalSeconds || 0);
  if (intervalSeconds <= 0 || estimateDurationSeconds.value <= 0) {
    return 0;
  }

  return Math.floor(estimateDurationSeconds.value / intervalSeconds) + 1;
});
const estimatedFrameStorageBytes = computed(() => {
  if (!config.value?.products?.keepFrames) {
    return 0;
  }

  return estimateBaselineAverageFrameBytes.value * estimatedFrameCount.value;
});
const estimatedTimelapseBytes = computed(() => {
  if (
    !config.value?.products?.timelapseEnabled ||
    estimatedFrameCount.value <= 0
  ) {
    return 0;
  }

  const fps = Math.max(1, Number(config.value?.products?.timelapseFps || 1));
  const bitrateKbps = Math.max(
    1000,
    Number(config.value?.products?.timelapseBitrateKbps || 1000),
  );
  const videoSeconds = estimatedFrameCount.value / fps;
  return Math.round((videoSeconds * bitrateKbps * 1000) / 8);
});
const estimatedKeogramBytes = computed(() =>
  config.value?.products?.keogramEnabled
    ? Number(estimateBaseline.value?.keogramBytes || 0)
    : 0,
);
const estimatedStartrailsBytes = computed(() =>
  config.value?.products?.startrailsEnabled
    ? Number(estimateBaseline.value?.startrailsBytes || 0)
    : 0,
);
const estimatedStorageBytes = computed(
  () =>
    estimatedFrameStorageBytes.value +
    estimatedTimelapseBytes.value +
    estimatedKeogramBytes.value +
    estimatedStartrailsBytes.value,
);
const estimateDurationLabel = computed(() =>
  formatDuration(estimateDurationSeconds.value),
);
const estimateExceedsAvailable = computed(() => {
  if (estimatedStorageBytes.value <= 0) {
    return false;
  }

  const diskAvailableBytes = Number(storage.value.diskAvailableBytes || 0);
  if (
    diskAvailableBytes > 0 &&
    estimatedStorageBytes.value > diskAvailableBytes
  ) {
    return true;
  }

  if (storage.value.limitEnabled) {
    const pluginAvailableBytes = Number(
      storage.value.pluginAvailableBytes || 0,
    );
    return estimatedStorageBytes.value > pluginAvailableBytes;
  }

  return false;
});
const estimateWarning = computed(() => {
  if (!parsedEstimateStart.value || !parsedEstimateEnd.value) {
    return tr("estimate.invalidWindow");
  }

  if (estimateDurationSeconds.value <= 0) {
    return tr("estimate.endBeforeStart");
  }

  if (!estimateBaseline.value) {
    return tr("estimate.noBaseline");
  }

  if (estimatedStorageBytes.value <= 0) {
    return null;
  }

  const warnings = [];
  const diskAvailableBytes = Number(storage.value.diskAvailableBytes || 0);
  if (
    diskAvailableBytes > 0 &&
    estimatedStorageBytes.value > diskAvailableBytes
  ) {
    warnings.push(
      tr("estimate.piFreeSpace", { size: formatSize(diskAvailableBytes) }),
    );
  }

  if (storage.value.limitEnabled) {
    const pluginAvailableBytes = Number(
      storage.value.pluginAvailableBytes || 0,
    );
    if (estimatedStorageBytes.value > pluginAvailableBytes) {
      warnings.push(
        tr("estimate.pluginLimitRemaining", {
          size: formatSize(pluginAvailableBytes),
        }),
      );
    }
  }

  if (warnings.length === 0) {
    return null;
  }

  return tr("estimate.exceeds", {
    size: formatSize(estimatedStorageBytes.value),
    limits: warnings.join(` ${tr("common.and")} `),
  });
});

const meteringOptions = computed(() => [
  { value: "centre", label: tr("modals.camera.options.centre") },
  { value: "spot", label: tr("modals.camera.options.spot") },
  { value: "average", label: tr("modals.camera.options.average") },
  { value: "custom", label: tr("modals.camera.options.custom") },
]);

const awbOptions = computed(() => [
  { value: "auto", label: tr("modals.camera.options.auto") },
  { value: "incandescent", label: tr("modals.camera.options.incandescent") },
  { value: "tungsten", label: tr("modals.camera.options.tungsten") },
  { value: "fluorescent", label: tr("modals.camera.options.fluorescent") },
  { value: "indoor", label: tr("modals.camera.options.indoor") },
  { value: "daylight", label: tr("modals.camera.options.daylight") },
  { value: "cloudy", label: tr("modals.camera.options.cloudy") },
  { value: "custom", label: tr("modals.camera.options.custom") },
]);

const denoiseOptions = computed(() => [
  { value: "auto", label: tr("modals.camera.options.auto") },
  { value: "off", label: tr("modals.camera.options.off") },
  { value: "cdn_off", label: tr("modals.camera.options.cdnOff") },
  { value: "cdn_fast", label: tr("modals.camera.options.cdnFast") },
  { value: "cdn_hq", label: tr("modals.camera.options.cdnHq") },
]);

const settingsModalTitle = computed(() => {
  switch (settingsModal.value) {
    case "automation":
      return tr("buttons.automation");
    case "camera":
      return tr("buttons.camera");
    case "outputs":
      return tr("buttons.outputs");
    default:
      return "";
  }
});

const toggleSection = (sectionKey) => {
  sectionOpen[sectionKey] = !sectionOpen[sectionKey];
};

const openSettingsModal = (modalKey) => {
  if (!config.value) {
    return;
  }

  settingsModalSnapshot.value = JSON.stringify(config.value);
  settingsModal.value = modalKey;
};

const restoreSettingsSnapshot = () => {
  if (!settingsModalSnapshot.value) {
    return;
  }

  config.value = JSON.parse(settingsModalSnapshot.value);
};

const closeSettingsModal = ({ restore = true } = {}) => {
  if (restore) {
    restoreSettingsSnapshot();
  }

  settingsModal.value = null;
  settingsModalSnapshot.value = null;
};

const saveSettingsModal = async () => {
  await saveConfig();

  if (!error.value) {
    settingsModal.value = null;
    settingsModalSnapshot.value = null;
  }
};

const setRootConfigValue = (key, value) => {
  if (!config.value) {
    return;
  }

  config.value[key] = value;
};

const setCameraSetting = (key, value) => {
  if (!config.value?.camera) {
    return;
  }

  config.value.camera[key] = value;
};

const setProductSetting = (key, value) => {
  if (!config.value?.products) {
    return;
  }

  config.value.products[key] = value;
};

const setAdvancedApiSetting = (key, value) => {
  if (!config.value?.advancedApi) {
    return;
  }

  config.value.advancedApi[key] = value;
};

const refreshAll = async () => {
  await store.refreshAll();
};

const saveConfig = async () => {
  await store.saveConfig();
};

const startSession = async () => {
  store.manualLabel =
    (store.manualLabel || "").trim() || defaultManualLabel.value;
  await store.startSession();
};

const stopSession = async () => {
  await store.stopSession(true);
};

const generateArtifacts = async (sessionId = null) => {
  await store.generateArtifacts(sessionId);
};

const updateBackend = async () => {
  await store.updateBackend();
};

const formatSessionReason = (reason) => {
  if (!reason) {
    return tr("recentSessions.reasons.unknown");
  }

  const normalizedReason = String(reason)
    .replace(/-([a-z])/g, (_, character) => character.toUpperCase())
    .replace(/[^a-zA-Z0-9]/g, "");

  switch (normalizedReason) {
    case "manualStart":
      return tr("recentSessions.reasons.manualStart");
    case "manualStop":
      return tr("recentSessions.reasons.manualStop");
    case "sequenceStart":
      return tr("recentSessions.reasons.sequenceStart");
    case "sequenceStop":
      return tr("recentSessions.reasons.sequenceStop");
    default:
      return reason;
  }
};

const deleteSession = async (session) => {
  const sessionName =
    session?.label || session?.id || tr("preview.noActiveSession");
  if (!window.confirm(tr("dialogs.deleteSession", { sessionName }))) {
    return;
  }

  await store.deleteSession(session.id);
};

const deleteAllSessions = async () => {
  if (!window.confirm(tr("dialogs.deleteAllSessions"))) {
    return;
  }

  await store.deleteAllSessions();
};

const deleteArtifact = async (session, artifact) => {
  if (!artifact?.relativePath) {
    return;
  }

  const artifactName =
    artifact.name ||
    artifact.relativePath.split("/").pop() ||
    tr("common.notGenerated");
  if (!window.confirm(tr("dialogs.deleteArtifact", { artifactName }))) {
    return;
  }

  await store.deleteArtifact(session.id, artifact.relativePath);
};

const deleteFrame = async (session, frame) => {
  if (!frame?.relativePath) {
    return;
  }

  const frameName =
    frame.name ||
    frame.relativePath.split("/").pop() ||
    tr("common.notAvailable");
  if (!window.confirm(tr("dialogs.deleteFrame", { frameName }))) {
    return;
  }

  await store.deleteFrame(session.id, frame.relativePath);
};

const downloadRelativePath = async (relativePath, fallbackName = null) => {
  const derivedName =
    fallbackName || relativePath?.split("/").pop() || tr("common.download");
  await store.downloadFile(relativePath, derivedName);
};

const sessionDetails = (sessionId) =>
  sessionDetailsById.value?.[sessionId] || null;
const sessionDetailsLoading = (sessionId) =>
  Boolean(detailsLoadingById.value?.[sessionId]);
const isSessionDetailsOpen = (sessionId) =>
  Boolean(sessionDetailOpen[sessionId]);
const refreshSessionDetails = async (sessionId) => {
  await store.fetchSessionDetails(sessionId);
};
const toggleSessionDetails = async (sessionId) => {
  sessionDetailOpen[sessionId] = !sessionDetailOpen[sessionId];
  if (sessionDetailOpen[sessionId]) {
    await store.fetchSessionDetails(sessionId);
  }
};

const parseDateValue = (value) => {
  if (!value) {
    return null;
  }

  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }

  if (typeof value === "string" || typeof value === "number") {
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  if (typeof value === "object") {
    const utcDateTime =
      typeof value.utcDateTime === "string" ? value.utcDateTime : null;
    if (utcDateTime) {
      const parsedUtc = new Date(
        /[zZ]|[+-]\d{2}:\d{2}$/.test(utcDateTime)
          ? utcDateTime
          : `${utcDateTime}Z`,
      );
      if (!Number.isNaN(parsedUtc.getTime())) {
        return parsedUtc;
      }
    }

    const localDateTime =
      typeof value.localDateTime === "string" ? value.localDateTime : null;
    if (localDateTime) {
      const parsedLocal = new Date(localDateTime);
      if (!Number.isNaN(parsedLocal.getTime())) {
        return parsedLocal;
      }
    }

    const dateTime = typeof value.dateTime === "string" ? value.dateTime : null;
    if (dateTime) {
      const parsedDateTime = new Date(dateTime);
      if (!Number.isNaN(parsedDateTime.getTime())) {
        return parsedDateTime;
      }
    }
  }

  return null;
};

const sameLocalDay = (left, right) =>
  left.getFullYear() === right.getFullYear() &&
  left.getMonth() === right.getMonth() &&
  left.getDate() === right.getDate();

const buildDefaultManualLabel = () => {
  const now = new Date();
  const datePrefix = `${String(now.getFullYear()).slice(-2)}${String(now.getMonth() + 1).padStart(2, "0")}${String(now.getDate()).padStart(2, "0")}`;
  const sessionsById = new Map();
  const candidateSessions = [
    currentSession.value,
    ...recentSessions.value,
  ].filter(Boolean);

  for (const session of candidateSessions) {
    const sessionKey =
      session.id ||
      `${session.label || ""}:${JSON.stringify(session.startedAtUtc || null)}`;
    if (!sessionsById.has(sessionKey)) {
      sessionsById.set(sessionKey, session);
    }
  }

  let sessionsStartedToday = 0;
  let maxLabelCounter = 0;

  for (const session of sessionsById.values()) {
    const startedAt = parseDateValue(session.startedAtUtc);
    if (startedAt && sameLocalDay(startedAt, now)) {
      sessionsStartedToday += 1;
    }

    const label = typeof session.label === "string" ? session.label.trim() : "";
    const match = label.match(new RegExp(`^${datePrefix}_(\\d+)$`));
    if (match) {
      maxLabelCounter = Math.max(maxLabelCounter, Number(match[1]));
    }
  }

  const nextCounter = Math.max(sessionsStartedToday, maxLabelCounter) + 1;
  return `${datePrefix}_${nextCounter}`;
};

const defaultManualLabel = computed(() => buildDefaultManualLabel());
const manualLabelInput = computed({
  get: () => store.manualLabel || defaultManualLabel.value,
  set: (value) => {
    store.manualLabel = typeof value === "string" ? value : "";
  },
});

const parseLocalInputValue = (value) => {
  if (!value) {
    return null;
  }

  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
};

const toLocalInputValue = (date) => {
  const localTime = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return localTime.toISOString().slice(0, 16);
};

const initializeEstimateWindow = () => {
  if (estimateWindow.startLocal && estimateWindow.endLocal) {
    return;
  }

  const now = new Date();
  const nextMorning = new Date(now);
  nextMorning.setDate(nextMorning.getDate() + 1);
  nextMorning.setHours(8, 0, 0, 0);

  estimateWindow.startLocal = toLocalInputValue(now);
  estimateWindow.endLocal = toLocalInputValue(nextMorning);
};

const formatDate = (value) => {
  const parsed = parseDateValue(value);
  if (!parsed) {
    return tr("common.notAvailable");
  }

  return parsed.toLocaleString();
};

const formatInterval = (seconds) => {
  if (!Number.isFinite(seconds)) {
    return tr("common.notAvailable");
  }

  return `${seconds}s`;
};

const formatExposure = (camera) => {
  if (!camera) {
    return tr("common.notAvailable");
  }

  if (!camera.useManualExposure) {
    return tr("common.auto");
  }

  const shutterMicroseconds = Number(camera.shutterMicroseconds || 0);
  if (!Number.isFinite(shutterMicroseconds) || shutterMicroseconds <= 0) {
    return tr("common.notAvailable");
  }

  const exposureSeconds = shutterMicroseconds / 1000000;
  if (exposureSeconds >= 1) {
    return `${exposureSeconds.toFixed(exposureSeconds >= 10 ? 0 : 1)}s`;
  }

  return `${exposureSeconds.toFixed(exposureSeconds >= 0.1 ? 2 : 3)}s`;
};

const formatGain = (camera) => {
  if (!camera) {
    return tr("common.notAvailable");
  }

  if (!camera.useManualGain) {
    return tr("common.auto");
  }

  const gain = Number(camera.analogGain || 0);
  if (!Number.isFinite(gain) || gain <= 0) {
    return tr("common.notAvailable");
  }

  return gain >= 10 ? gain.toFixed(0) : gain.toFixed(1).replace(/\.0$/, "");
};

const formatCount = (value) => {
  if (!Number.isFinite(value)) {
    return tr("common.notAvailable");
  }

  return new Intl.NumberFormat().format(value);
};

const formatDuration = (seconds) => {
  if (!Number.isFinite(seconds) || seconds <= 0) {
    return tr("common.notAvailable");
  }

  const totalMinutes = Math.round(seconds / 60);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours === 0) {
    return `${minutes}m`;
  }

  if (minutes === 0) {
    return `${hours}h`;
  }

  return `${hours}h ${minutes}m`;
};

const formatSize = (bytes) => {
  if (!bytes && bytes !== 0) {
    return tr("common.notAvailable");
  }

  const units = ["B", "KB", "MB", "GB", "TB"];
  let value = bytes;
  let unitIndex = 0;

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  const precision = unitIndex === 0 ? 0 : value >= 100 ? 0 : 1;
  return `${value.toFixed(precision)} ${units[unitIndex]}`;
};

const describeArtifact = (artifact) => {
  if (!artifact) {
    return tr("common.notGenerated");
  }

  return `${formatDate(artifact.generatedAtUtc)} • ${formatSize(artifact.sizeBytes)}`;
};

onMounted(async () => {
  initializeEstimateWindow();
  await refreshAll();
  store.startPolling();
});

onBeforeUnmount(() => {
  store.stopPolling();
});
</script>
