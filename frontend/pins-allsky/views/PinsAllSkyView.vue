<template>
  <div class="container py-8 sm:py-12 px-4">
    <div class="mx-auto max-w-7xl space-y-6">
      <section class="rounded-2xl border border-cyan-900/40 bg-gray-800/80 p-6 shadow-2xl">
        <button
          type="button"
          class="flex w-full items-start justify-between gap-4 text-left"
          @click="toggleSection('overview')"
        >
          <div>
            <h1 class="text-3xl font-bold text-white">AllSky Capture</h1>
            <p class="mt-2 max-w-3xl text-sm text-gray-300">
              Control the Pi HQ camera during a normal imaging run and build timelapse videos,
              keograms, and startrail composites from the captured frames.
            </p>
          </div>
          <div class="flex items-center gap-3">
            <span class="rounded-full border border-gray-600 bg-gray-900/70 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-gray-300">
              {{ sectionOpen.overview ? 'Expanded' : 'Collapsed' }}
            </span>
            <span class="flex h-9 w-9 items-center justify-center rounded-full border border-cyan-500/40 bg-cyan-500/10 text-lg font-semibold text-cyan-200">
              {{ sectionOpen.overview ? '-' : '+' }}
            </span>
          </div>
        </button>

        <div v-if="sectionOpen.overview" class="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-7">
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-400">Backend</div>
            <div class="mt-2 text-sm font-semibold" :class="status?.advancedApiReachable ? 'text-emerald-400' : 'text-amber-300'">
              {{ status ? (status.advancedApiReachable ? 'Online' : 'Offline') : 'Loading' }}
            </div>
          </div>
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-400">Sequence</div>
            <div class="mt-2 text-sm font-semibold" :class="status?.sequenceRunning ? 'text-emerald-400' : 'text-gray-300'">
              {{ status?.sequenceRunning ? 'Running' : 'Idle' }}
            </div>
          </div>
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-400">Capture</div>
            <div class="mt-2 text-sm font-semibold" :class="status?.captureRunning ? 'text-emerald-400' : 'text-gray-300'">
              {{ status?.captureRunning ? 'Active' : 'Stopped' }}
            </div>
          </div>
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-400">Products</div>
            <div class="mt-2 text-sm font-semibold" :class="status?.generateInProgress ? 'text-cyan-300' : 'text-gray-300'">
              {{ status?.generateInProgress ? 'Rendering' : 'Ready' }}
            </div>
          </div>
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-400">Pi Used</div>
            <div class="mt-2 text-sm font-semibold text-white">
              {{ formatSize(storage.diskUsedBytes) }}
            </div>
          </div>
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-400">Pi Available</div>
            <div class="mt-2 text-sm font-semibold" :class="estimateExceedsAvailable ? 'text-amber-300' : 'text-white'">
              {{ formatSize(storage.diskAvailableBytes) }}
            </div>
          </div>
          <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
            <div class="text-xs uppercase tracking-wide text-gray-400">Plugin Used</div>
            <div class="mt-2 text-sm font-semibold text-white">
              {{ formatSize(storage.pluginUsedBytes) }}
            </div>
          </div>
        </div>
      </section>

      <div
        v-if="error"
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
        v-if="missingDependencies.length > 0"
        class="rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-100"
      >
        Missing runtime dependencies:
        <span class="font-semibold">{{ missingDependencies.join(', ') }}</span>
      </div>

      <div class="grid gap-6 xl:grid-cols-[1.3fr_1fr]">
        <section class="rounded-2xl border border-gray-700 bg-gray-800/80 p-5 shadow-xl">
          <div class="flex items-center justify-between gap-3">
            <div>
              <h2 class="text-xl font-semibold text-white">Live Preview</h2>
              <p class="text-sm text-gray-400">
                Latest captured frame from the current or most recent session.
              </p>
            </div>
            <button
              class="rounded-lg border border-cyan-500/40 bg-cyan-500/10 px-3 py-2 text-sm font-medium text-cyan-200 transition hover:bg-cyan-500/20"
              @click="refreshAll"
            >
              Refresh
            </button>
          </div>

          <div class="mt-5 overflow-hidden rounded-2xl border border-gray-700 bg-black/60">
            <img
              v-if="currentImageUrl"
              :src="currentImageUrl"
              alt="Latest all-sky frame"
              class="h-[28rem] w-full object-contain"
            />
            <div
              v-else
              class="flex h-[28rem] items-center justify-center px-6 text-center text-sm text-gray-500"
            >
              No captured frame is available yet. Start a session or wait for the next automatic
              capture.
            </div>
          </div>

          <div class="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
            <div class="rounded-xl border border-gray-700 bg-gray-900/50 p-3">
              <div class="text-xs uppercase tracking-wide text-gray-500">Session</div>
              <div class="mt-2 text-sm text-white">
                {{ currentSession?.id || 'No active session' }}
              </div>
            </div>
            <div class="rounded-xl border border-gray-700 bg-gray-900/50 p-3">
              <div class="text-xs uppercase tracking-wide text-gray-500">Frames</div>
              <div class="mt-2 text-sm text-white">{{ currentSession?.captureCount || 0 }}</div>
            </div>
            <div class="rounded-xl border border-gray-700 bg-gray-900/50 p-3">
              <div class="text-xs uppercase tracking-wide text-gray-500">Last Capture</div>
              <div class="mt-2 text-sm text-white">{{ formatDate(currentSession?.lastCaptureAtUtc) }}</div>
            </div>
            <div class="rounded-xl border border-gray-700 bg-gray-900/50 p-3">
              <div class="text-xs uppercase tracking-wide text-gray-500">Capture Interval</div>
              <div class="mt-2 text-sm text-white">{{ formatInterval(config?.camera?.intervalSeconds) }}</div>
            </div>
            <div class="rounded-xl border border-gray-700 bg-gray-900/50 p-3">
              <div class="text-xs uppercase tracking-wide text-gray-500">Sequence Poll</div>
              <div class="mt-2 text-sm text-white">{{ formatInterval(config?.sequencePollIntervalSeconds) }}</div>
            </div>
          </div>
        </section>

        <section class="rounded-2xl border border-gray-700 bg-gray-800/80 p-5 shadow-xl">
          <h2 class="text-xl font-semibold text-white">Session Control</h2>
          <p class="mt-1 text-sm text-gray-400">
            Manual sessions can run alongside automatic sequence-triggered operation.
          </p>

          <div class="mt-5 space-y-4">
            <label class="block">
              <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                Manual Session Label
              </span>
              <input
                v-model="store.manualLabel"
                title="Optional label used to identify the next manually started AllSky session."
                class="w-full rounded-xl border border-gray-600 bg-gray-900/80 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                placeholder="Optional label for the next manual session"
              />
            </label>

            <div class="grid gap-3 sm:grid-cols-3">
              <button
                class="rounded-xl bg-emerald-600 px-4 py-3 font-semibold text-white transition hover:bg-emerald-500 disabled:cursor-not-allowed disabled:opacity-40"
                :disabled="loading || status?.captureRunning"
                @click="startSession"
              >
                Start Capture
              </button>
              <button
                class="rounded-xl border border-cyan-500/40 bg-cyan-500/10 px-4 py-3 font-semibold text-cyan-100 transition hover:bg-cyan-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                :disabled="loading || !status?.captureRunning || !currentSession?.captureCount || status?.generateInProgress"
                @click="generateArtifacts(currentSession?.id || null)"
              >
                {{ status?.generateInProgress ? 'Rendering Progress…' : 'Render Progress' }}
              </button>
              <button
                class="rounded-xl bg-rose-600 px-4 py-3 font-semibold text-white transition hover:bg-rose-500 disabled:cursor-not-allowed disabled:opacity-40"
                :disabled="loading || !status?.captureRunning"
                @click="stopSession"
              >
                Stop And Render
              </button>
            </div>

            <p class="text-xs text-gray-500">
              `Render Progress` generates timelapse, keogram, and startrails from frames captured
              so far without stopping the active capture session.
            </p>

            <button
              class="w-full rounded-xl border border-cyan-500/40 bg-cyan-500/10 px-4 py-3 font-semibold text-cyan-100 transition hover:bg-cyan-500/20 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="loading || status?.generateInProgress"
              @click="generateArtifacts()"
            >
              Regenerate Latest Artifacts
            </button>
          </div>

          <div class="mt-6 rounded-xl border border-gray-700 bg-gray-900/60 p-4">
            <button
              type="button"
              class="flex w-full items-center justify-between gap-3 text-left"
              @click="toggleSection('dependencies')"
            >
              <div class="text-sm font-semibold text-white">Backend Dependencies</div>
              <div class="flex items-center gap-3">
                <span class="text-xs uppercase tracking-wide text-gray-400">
                  {{ sectionOpen.dependencies ? 'Expanded' : 'Collapsed' }}
                </span>
                <span class="flex h-7 w-7 items-center justify-center rounded-full border border-gray-600 bg-gray-800/80 text-sm font-semibold text-gray-200">
                  {{ sectionOpen.dependencies ? '-' : '+' }}
                </span>
              </div>
            </button>
            <div v-if="sectionOpen.dependencies" class="mt-3 grid gap-2">
              <div
                v-for="item in dependencyRows"
                :key="item.label"
                class="flex items-center justify-between rounded-lg border border-gray-700 bg-gray-800/70 px-3 py-2 text-sm"
              >
                <span class="text-gray-300">{{ item.label }}</span>
                <span :class="item.ready ? 'text-emerald-400' : 'text-amber-300'">
                  {{ item.ready ? 'Available' : 'Missing' }}
                </span>
              </div>
            </div>
          </div>
        </section>
      </div>

      <section v-if="config" class="rounded-2xl border border-gray-700 bg-gray-800/80 p-6 shadow-xl">
        <button
          type="button"
          class="flex w-full flex-col gap-4 text-left lg:flex-row lg:items-start lg:justify-between"
          @click="toggleSection('captureSettings')"
        >
          <div>
            <h2 class="text-xl font-semibold text-white">Capture And Product Settings</h2>
            <p class="text-sm text-gray-400">
              The backend persists these settings on the Pi and uses them for automatic sequence
              monitoring as well as manual runs.
            </p>
          </div>
          <div class="flex items-center gap-3 self-start lg:self-auto">
            <span class="rounded-full border border-gray-600 bg-gray-900/70 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-gray-300">
              {{ sectionOpen.captureSettings ? 'Expanded' : 'Collapsed' }}
            </span>
            <span class="flex h-9 w-9 items-center justify-center rounded-full border border-cyan-500/40 bg-cyan-500/10 text-lg font-semibold text-cyan-200">
              {{ sectionOpen.captureSettings ? '-' : '+' }}
            </span>
          </div>
        </button>

        <div v-if="sectionOpen.captureSettings" class="mt-6">
          <div class="flex justify-end">
            <button
              class="rounded-xl bg-cyan-600 px-5 py-3 font-semibold text-white transition hover:bg-cyan-500 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="saving"
              @click="saveConfig"
            >
              {{ saving ? 'Saving…' : 'Save Settings' }}
            </button>
          </div>

          <div class="mt-6 grid gap-6 xl:grid-cols-3">
          <div class="space-y-4 rounded-2xl border border-gray-700 bg-gray-900/50 p-4">
            <h3 class="text-lg font-semibold text-white">Automation</h3>
            <label class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2">
              <span class="text-sm text-gray-300">Auto-start with sequence</span>
              <toggleButton
                :status-value="Boolean(config.autoStartWithSequence)"
                @update:statusValue="setRootConfigValue('autoStartWithSequence', $event)"
              />
            </label>
            <div class="rounded-xl border border-cyan-500/20 bg-cyan-500/10 px-3 py-2 text-sm text-cyan-100">
              When enabled, AllSky starts a capture session as soon as the PINS Advanced API reports
              an active sequence, then stops capture and renders products automatically when the
              sequence ends.
            </div>
            <label class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2">
              <span class="text-sm text-gray-300">Advanced API enabled</span>
              <toggleButton
                :status-value="Boolean(config.advancedApi.enabled)"
                @update:statusValue="setAdvancedApiSetting('enabled', $event)"
              />
            </label>
            <label class="block">
              <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                Protocol
              </span>
              <input
                v-model="config.advancedApi.protocol"
                title="Protocol used to reach the local Advanced API exposed by PINS."
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
            </label>
            <label class="block">
              <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                Sequence Poll Interval (s)
              </span>
              <input
                v-model.number="config.sequencePollIntervalSeconds"
                type="number"
                min="5"
                max="300"
                title="How often AllSky checks whether a PINS/NINA sequence has started or stopped."
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
              <span class="mt-2 block text-xs text-gray-500">
                Controls how often AllSky checks whether a PINS/NINA sequence has started or stopped.
                It does not change the camera frame cadence.
              </span>
            </label>
            <label class="block">
              <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                Advanced API Host
              </span>
              <input
                v-model="config.advancedApi.host"
                title="Hostname or IP address of the PINS Advanced API service."
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
            </label>
            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Port
                </span>
                <input
                  v-model.number="config.advancedApi.port"
                  type="number"
                  title="TCP port used by the PINS Advanced API service."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Timeout (s)
                </span>
                <input
                  v-model.number="config.advancedApi.requestTimeoutSeconds"
                  type="number"
                  min="1"
                  max="30"
                  title="Maximum time to wait for a single Advanced API request before marking it unavailable."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
            </div>
            <label class="block">
              <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                Advanced API Base Path
              </span>
              <input
                v-model="config.advancedApi.basePath"
                title="Base path prefix used when calling the Advanced API endpoints."
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
            </label>
            <label class="block">
              <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                Max Plugin Storage (GB)
              </span>
              <input
                v-model.number="config.storage.maxUsageGb"
                type="number"
                min="0"
                step="0.1"
                inputmode="decimal"
                title="Maximum storage PINS AllSky may use under its plugin data directory. Set to 0 for no limit. When the limit is exceeded, the backend prunes the oldest completed sessions."
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
              <span class="mt-2 block text-xs text-gray-500">
                Set to <code class="font-mono text-gray-400">0</code> for no limit. Otherwise the
                backend deletes the oldest completed sessions when usage grows beyond this cap.
              </span>
            </label>

            <div class="space-y-4 rounded-xl border border-gray-700 bg-gray-800/70 p-4">
              <div>
                <div class="text-sm font-semibold text-white">Session Estimate</div>
                <p class="mt-1 text-xs text-gray-400">
                  Uses the average stored frame size from
                  <span class="font-semibold text-gray-200">{{ estimateBaselineLabel }}</span>
                  to estimate session storage before capture starts.
                </p>
              </div>

              <div class="grid gap-4 sm:grid-cols-2">
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Start
                  </span>
                  <input
                    v-model="estimateWindow.startLocal"
                    type="datetime-local"
                    title="Planned capture start time used for the storage estimate. Defaults to the current local time."
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/80 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    End
                  </span>
                  <input
                    v-model="estimateWindow.endLocal"
                    type="datetime-local"
                    title="Planned capture stop time used for the storage estimate. Defaults to tomorrow at 08:00 local time."
                    class="w-full rounded-xl border border-gray-600 bg-gray-900/80 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
              </div>

              <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
                  <div class="text-xs uppercase tracking-wide text-gray-500">Expected Duration</div>
                  <div class="mt-2 text-sm text-white">{{ estimateDurationLabel }}</div>
                </div>
                <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
                  <div class="text-xs uppercase tracking-wide text-gray-500">Expected Frames</div>
                  <div class="mt-2 text-sm text-white">{{ formatCount(estimatedFrameCount) }}</div>
                </div>
                <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
                  <div class="text-xs uppercase tracking-wide text-gray-500">Expected Storage</div>
                  <div class="mt-2 text-sm font-semibold" :class="estimateExceedsAvailable ? 'text-amber-300' : 'text-white'">
                    {{ formatSize(estimatedStorageBytes) }}
                  </div>
                </div>
                <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3">
                  <div class="text-xs uppercase tracking-wide text-gray-500">Frame Baseline</div>
                  <div class="mt-2 text-sm text-white">{{ formatSize(estimateBaselineAverageFrameBytes) }}</div>
                </div>
              </div>

              <div class="rounded-xl border border-gray-700 bg-gray-900/60 p-3 text-xs text-gray-400">
                Uses <code class="font-mono text-gray-300">{{ formatInterval(config.camera.intervalSeconds) }}</code>
                frame cadence. Timelapse size is estimated from the configured bitrate/FPS. Keogram
                and startrails reuse the last baseline output sizes.
              </div>

              <div
                v-if="estimateWarning"
                class="rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-100"
              >
                {{ estimateWarning }}
              </div>
            </div>
          </div>

          <div class="space-y-4 rounded-2xl border border-gray-700 bg-gray-900/50 p-4">
            <h3 class="text-lg font-semibold text-white">Camera</h3>
            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Interval (s)
                </span>
                <input
                  v-model.number="config.camera.intervalSeconds"
                  type="number"
                  min="5"
                  title="Time between capture starts in seconds. This is the actual AllSky frame cadence."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Timeout (s)
                </span>
                <input
                  v-model.number="config.camera.captureTimeoutSeconds"
                  type="number"
                  min="15"
                  title="Maximum time allowed for a single rpicam-still capture before it is treated as failed."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Width
                </span>
                <input
                  v-model.number="config.camera.width"
                  type="number"
                  min="640"
                  title="Output image width in pixels for each captured frame."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Height
                </span>
                <input
                  v-model.number="config.camera.height"
                  type="number"
                  min="480"
                  title="Output image height in pixels for each captured frame."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  JPEG Quality
                </span>
                <input
                  v-model.number="config.camera.quality"
                  type="number"
                  min="1"
                  max="100"
                  title="JPEG quality used for captured frames. Higher values preserve more detail at larger file sizes."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Warmup (ms)
                </span>
                <input
                  v-model.number="config.camera.warmupMilliseconds"
                  type="number"
                  min="1"
                  title="Warmup time passed to rpicam-still before each frame is written."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
            </div>

            <div class="grid gap-4 sm:grid-cols-2">
              <div class="space-y-3 rounded-xl border border-gray-700 bg-gray-800/70 p-3">
                <label class="flex items-center justify-between gap-3">
                  <span class="text-sm text-gray-300">Manual exposure</span>
                  <toggleButton
                    :status-value="Boolean(config.camera.useManualExposure)"
                    @update:statusValue="setCameraSetting('useManualExposure', $event)"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Shutter (µs)
                  </span>
                  <input
                    v-model.number="config.camera.shutterMicroseconds"
                    type="number"
                    min="1"
                    inputmode="numeric"
                    :disabled="!config.camera.useManualExposure"
                    title="Manual shutter time in microseconds. Only applied when Manual exposure is enabled."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400 disabled:cursor-not-allowed disabled:opacity-50"
                  />
                </label>
                <p class="text-xs text-gray-500">
                  When disabled, <code class="font-mono text-gray-400">rpicam-still</code> controls exposure automatically.
                </p>
              </div>

              <div class="space-y-3 rounded-xl border border-gray-700 bg-gray-800/70 p-3">
                <label class="flex items-center justify-between gap-3">
                  <span class="text-sm text-gray-300">Manual gain</span>
                  <toggleButton
                    :status-value="Boolean(config.camera.useManualGain)"
                    @update:statusValue="setCameraSetting('useManualGain', $event)"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Analog Gain
                  </span>
                  <input
                    v-model.number="config.camera.analogGain"
                    type="number"
                    min="1"
                    step="0.1"
                    inputmode="decimal"
                    :disabled="!config.camera.useManualGain"
                    title="Manual analog gain applied to the Pi camera. Only used when Manual gain is enabled."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400 disabled:cursor-not-allowed disabled:opacity-50"
                  />
                </label>
                <p class="text-xs text-gray-500">
                  When disabled, <code class="font-mono text-gray-400">rpicam-still</code> controls gain automatically.
                </p>
              </div>
            </div>

            <div class="grid gap-4 sm:grid-cols-2">
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Metering
                </span>
                <select
                  v-model="config.camera.meteringMode"
                  title="Exposure metering strategy used by rpicam-still."
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
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  AWB
                </span>
                <select
                  v-model="config.camera.awbMode"
                  title="Automatic white balance mode used by rpicam-still."
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
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Denoise
                </span>
                <select
                  v-model="config.camera.denoiseMode"
                  title="Noise reduction mode used by rpicam-still for each captured frame."
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
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  EV Compensation
                </span>
                <input
                  v-model.number="config.camera.evCompensation"
                  type="number"
                  step="0.1"
                  inputmode="decimal"
                  title="Exposure value compensation applied on top of the selected exposure mode."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Rotation
                </span>
                <input
                  v-model.number="config.camera.rotation"
                  type="number"
                  min="0"
                  max="180"
                  title="Image rotation in degrees. The Pi camera stack only supports 0 or 180 here."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Brightness
                </span>
                <input
                  v-model.number="config.camera.brightness"
                  type="number"
                  step="0.1"
                  inputmode="decimal"
                  title="Brightness adjustment applied by rpicam-still."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Contrast
                </span>
                <input
                  v-model.number="config.camera.contrast"
                  type="number"
                  min="0"
                  step="0.1"
                  inputmode="decimal"
                  title="Contrast multiplier applied by rpicam-still."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Saturation
                </span>
                <input
                  v-model.number="config.camera.saturation"
                  type="number"
                  min="0"
                  step="0.1"
                  inputmode="decimal"
                  title="Saturation multiplier applied by rpicam-still."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
              <label class="block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Sharpness
                </span>
                <input
                  v-model.number="config.camera.sharpness"
                  type="number"
                  min="0"
                  step="0.1"
                  inputmode="decimal"
                  title="Sharpness multiplier applied by rpicam-still."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
            </div>

            <div class="rounded-xl border border-cyan-500/20 bg-cyan-500/10 px-3 py-2 text-sm text-cyan-100">
              <code class="font-mono">rpicam-still</code> rotation is limited to <code class="font-mono">0</code> or <code class="font-mono">180</code> degrees.
            </div>

            <div class="grid gap-3 sm:grid-cols-2">
              <label class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2">
                <span class="text-sm text-gray-300">Horizontal flip</span>
                <toggleButton
                  :status-value="Boolean(config.camera.horizontalFlip)"
                  @update:statusValue="setCameraSetting('horizontalFlip', $event)"
                />
              </label>
              <label class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2">
                <span class="text-sm text-gray-300">Vertical flip</span>
                <toggleButton
                  :status-value="Boolean(config.camera.verticalFlip)"
                  @update:statusValue="setCameraSetting('verticalFlip', $event)"
                />
              </label>
            </div>

            <label class="block">
              <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                Extra rpicam-still Arguments
              </span>
              <textarea
                v-model="config.camera.extraArguments"
                rows="3"
                title="Additional raw rpicam-still arguments appended after the managed camera settings."
                class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
              />
            </label>
          </div>

          <div class="space-y-4 rounded-2xl border border-gray-700 bg-gray-900/50 p-4">
            <h3 class="text-lg font-semibold text-white">Products</h3>
            <label class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-800/70 px-3 py-2">
              <span class="text-sm text-gray-300">Keep source frames</span>
              <toggleButton
                :status-value="Boolean(config.products.keepFrames)"
                @update:statusValue="setProductSetting('keepFrames', $event)"
              />
            </label>

            <div class="rounded-xl border border-gray-700 bg-gray-800/70 p-3">
              <div class="flex items-center justify-between gap-3">
                <span class="font-semibold text-white">Timelapse</span>
                <toggleButton
                  :status-value="Boolean(config.products.timelapseEnabled)"
                  @update:statusValue="setProductSetting('timelapseEnabled', $event)"
                />
              </div>
              <div v-if="config.products.timelapseEnabled" class="mt-3 grid gap-4 sm:grid-cols-2">
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    FPS
                  </span>
                  <input
                    v-model.number="config.products.timelapseFps"
                    type="number"
                    min="1"
                    max="60"
                    title="Playback frame rate used when assembling the timelapse video."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Bitrate (kbps)
                  </span>
                  <input
                    v-model.number="config.products.timelapseBitrateKbps"
                    type="number"
                    min="1000"
                    title="Target video bitrate for the generated timelapse in kilobits per second."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Width
                  </span>
                  <input
                    v-model.number="config.products.timelapseWidth"
                    type="number"
                    min="320"
                    title="Output width in pixels for the rendered timelapse video."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Height
                  </span>
                  <input
                    v-model.number="config.products.timelapseHeight"
                    type="number"
                    min="240"
                    title="Output height in pixels for the rendered timelapse video."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Codec
                  </span>
                  <input
                    v-model="config.products.timelapseCodec"
                    title="ffmpeg video codec used to encode the timelapse output."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Pixel Format
                  </span>
                  <input
                    v-model="config.products.timelapsePixelFormat"
                    title="ffmpeg pixel format used for the timelapse output."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    FFmpeg Log Level
                  </span>
                  <input
                    v-model="config.products.timelapseLogLevel"
                    title="ffmpeg log verbosity used while rendering the timelapse."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
              </div>
              <label v-if="config.products.timelapseEnabled" class="mt-3 block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Extra ffmpeg Arguments
                </span>
                <textarea
                  v-model="config.products.timelapseExtraParameters"
                  rows="3"
                  title="Extra ffmpeg arguments appended to the timelapse render command."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
            </div>

            <div class="rounded-xl border border-gray-700 bg-gray-800/70 p-3">
              <div class="flex items-center justify-between gap-3">
                <span class="font-semibold text-white">Keogram</span>
                <toggleButton
                  :status-value="Boolean(config.products.keogramEnabled)"
                  @update:statusValue="setProductSetting('keogramEnabled', $event)"
                />
              </div>
              <div v-if="config.products.keogramEnabled" class="mt-3 grid gap-4 sm:grid-cols-2">
                <label class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2">
                  <span class="text-sm text-gray-300">Expand to frame width</span>
                  <toggleButton
                    :status-value="Boolean(config.products.keogramExpand)"
                    @update:statusValue="setProductSetting('keogramExpand', $event)"
                  />
                </label>
                <label class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2">
                  <span class="text-sm text-gray-300">Show labels</span>
                  <toggleButton
                    :status-value="Boolean(config.products.keogramShowLabels)"
                    @update:statusValue="setProductSetting('keogramShowLabels', $event)"
                  />
                </label>
                <label class="flex items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-900/60 px-3 py-2">
                  <span class="text-sm text-gray-300">Show date</span>
                  <toggleButton
                    :status-value="Boolean(config.products.keogramShowDate)"
                    @update:statusValue="setProductSetting('keogramShowDate', $event)"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Rotate (deg)
                  </span>
                  <input
                    v-model.number="config.products.keogramRotateDegrees"
                    type="number"
                    inputmode="decimal"
                    title="Rotation applied to the finished keogram image."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Font Name
                  </span>
                  <input
                    v-model="config.products.keogramFontName"
                    title="Font family name passed to the AllSky keogram tool for labels."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Font Color
                  </span>
                  <input
                    v-model="config.products.keogramFontColor"
                    title="Font color used by the keogram tool, usually as a hex color value."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Font Size
                  </span>
                  <input
                    v-model.number="config.products.keogramFontSize"
                    type="number"
                    min="0.1"
                    step="0.1"
                    inputmode="decimal"
                    title="Label font size used in the generated keogram."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Line Thickness
                  </span>
                  <input
                    v-model.number="config.products.keogramLineThickness"
                    type="number"
                    min="1"
                    title="Line thickness used when the keogram tool draws labels or markers."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
              </div>
              <label v-if="config.products.keogramEnabled" class="mt-3 block">
                <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                  Extra keogram Arguments
                </span>
                <textarea
                  v-model="config.products.keogramExtraParameters"
                  rows="3"
                  title="Extra command-line arguments appended to the AllSky keogram tool."
                  class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                />
              </label>
            </div>

            <div class="rounded-xl border border-gray-700 bg-gray-800/70 p-3">
              <div class="flex items-center justify-between gap-3">
                <span class="font-semibold text-white">Startrails</span>
                <toggleButton
                  :status-value="Boolean(config.products.startrailsEnabled)"
                  @update:statusValue="setProductSetting('startrailsEnabled', $event)"
                />
              </div>
              <div v-if="config.products.startrailsEnabled" class="mt-3 space-y-3">
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Brightness Threshold
                  </span>
                  <input
                    v-model.number="config.products.startrailsBrightnessThreshold"
                    type="number"
                    min="0"
                    max="1"
                    step="0.01"
                    inputmode="decimal"
                    title="Minimum normalized brightness a pixel must reach before it contributes to the startrails composite."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
                <div class="rounded-xl border border-amber-500/20 bg-amber-500/10 px-3 py-2 text-sm text-amber-100">
                  On an equatorial mount the camera may track with the sky, so the startrail output
                  can still be generated but may not show the classic circular trail effect.
                </div>
                <label class="block">
                  <span class="mb-2 block text-xs font-semibold uppercase tracking-wide text-gray-400">
                    Extra startrails Arguments
                  </span>
                  <textarea
                    v-model="config.products.startrailsExtraParameters"
                    rows="3"
                    title="Extra command-line arguments appended to the AllSky startrails tool."
                    class="w-full rounded-xl border border-gray-600 bg-gray-800/70 px-3 py-2 text-white outline-none transition focus:border-cyan-400"
                  />
                </label>
              </div>
            </div>
          </div>
        </div>
        </div>
      </section>

      <section class="rounded-2xl border border-gray-700 bg-gray-800/80 p-6 shadow-xl">
        <button
          type="button"
          class="flex w-full items-start justify-between gap-4 text-left"
          @click="toggleSection('recentSessions')"
        >
          <div>
            <h2 class="text-xl font-semibold text-white">Recent Sessions</h2>
            <p class="mt-1 text-sm text-gray-400">
              Generated products are served directly from the backend and open in a new tab.
            </p>
          </div>
          <div class="flex items-center gap-3">
            <span class="rounded-full border border-gray-600 bg-gray-900/70 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-gray-300">
              {{ sectionOpen.recentSessions ? 'Expanded' : 'Collapsed' }}
            </span>
            <span class="flex h-9 w-9 items-center justify-center rounded-full border border-cyan-500/40 bg-cyan-500/10 text-lg font-semibold text-cyan-200">
              {{ sectionOpen.recentSessions ? '-' : '+' }}
            </span>
          </div>
        </button>

        <div v-if="sectionOpen.recentSessions" class="mt-6 space-y-4">
          <div class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-900/50 px-4 py-3">
            <div class="text-sm text-gray-300">
              <span :class="storage.withinLimit ? 'text-emerald-300' : 'text-amber-200'">
                {{ storage.withinLimit || !storage.limitEnabled ? 'Storage within limit.' : 'Storage limit exceeded.' }}
              </span>
              <span class="text-gray-500">
                Plugin available:
                {{ storage.limitEnabled ? formatSize(storage.pluginAvailableBytes) : 'Unlimited' }}.
              </span>
              <span class="text-gray-500">
                Pi available: {{ formatSize(storage.diskAvailableBytes) }}.
              </span>
            </div>
            <button
              class="inline-flex h-10 w-10 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="cleanupBusy || status?.captureRunning || status?.generateInProgress || recentSessions.length === 0"
              title="Delete all stored sessions, frames, and generated products"
              aria-label="Delete all sessions"
              @click.stop="deleteAllSessions"
            >
              <TrashIcon class="h-5 w-5" />
            </button>
          </div>

          <div
            v-if="actionMessage"
            class="rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-100"
          >
            {{ actionMessage }}
          </div>
        </div>

        <div v-if="sectionOpen.recentSessions && recentSessions.length === 0" class="mt-6 rounded-xl border border-dashed border-gray-700 bg-gray-900/40 px-4 py-8 text-center text-sm text-gray-500">
          No completed sessions have been recorded yet.
        </div>

        <div v-else-if="sectionOpen.recentSessions" class="mt-6 space-y-4">
          <article
            v-for="session in recentSessions"
            :key="session.id"
            class="rounded-2xl border border-gray-700 bg-gray-900/50 p-4"
          >
            <div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
              <div class="space-y-1">
                <div class="text-lg font-semibold text-white">
                  {{ session.label || session.id }}
                </div>
                <div class="text-sm text-gray-400">
                  Started {{ formatDate(session.startedAtUtc) }}
                  <span v-if="session.endedAtUtc"> • Stopped {{ formatDate(session.endedAtUtc) }}</span>
                </div>
                <div class="text-sm text-gray-400">
                  Frames: {{ session.captureCount }} • Reason: {{ session.startReason }}
                  <span v-if="session.stopReason"> • {{ session.stopReason }}</span>
                </div>
                <div class="text-sm text-gray-400">
                  Storage: {{ formatSize(session.totalSizeBytes) }}
                </div>
              </div>

              <div class="flex flex-wrap gap-2">
                <button
                  class="rounded-lg border border-cyan-500/40 bg-cyan-500/10 px-3 py-2 text-sm font-medium text-cyan-100 transition hover:bg-cyan-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                  :disabled="cleanupBusy"
                  @click="generateArtifacts(session.id)"
                >
                  Regenerate
                </button>
                <button
                  class="inline-flex h-10 w-10 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                  :disabled="cleanupBusy || status?.captureRunning || status?.generateInProgress || session.id === currentSession?.id"
                  title="Delete this session"
                  aria-label="Delete this session"
                  @click="deleteSession(session)"
                >
                  <TrashIcon class="h-5 w-5" />
                </button>
                <button
                  class="inline-flex h-10 items-center justify-center gap-2 rounded-lg border border-gray-600 bg-gray-800 px-3 py-2 text-sm text-white transition hover:border-cyan-400"
                  :title="isSessionDetailsOpen(session.id) ? 'Hide stored files' : 'Show stored files'"
                  :aria-label="isSessionDetailsOpen(session.id) ? 'Hide stored files' : 'Show stored files'"
                  @click="toggleSessionDetails(session.id)"
                >
                  <span>Files</span>
                  <ChevronUpIcon v-if="isSessionDetailsOpen(session.id)" class="h-4 w-4" />
                  <ChevronDownIcon v-else class="h-4 w-4" />
                </button>
              </div>
            </div>

            <div class="mt-4 grid gap-3 sm:grid-cols-3">
              <div class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300">
                <div class="text-xs uppercase tracking-wide text-gray-500">Timelapse</div>
                <div class="mt-2 flex items-start justify-between gap-3">
                  <div>{{ describeArtifact(session.products?.timelapse) }}</div>
                  <div v-if="session.products?.timelapse" class="flex items-center gap-2">
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
                      title="Download timelapse"
                      aria-label="Download timelapse"
                      @click="downloadRelativePath(session.products.timelapse.relativePath)"
                    >
                      <ArrowDownTrayIcon class="h-4 w-4" />
                    </button>
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                      :disabled="cleanupBusy || status?.generateInProgress || session.id === currentSession?.id"
                      title="Delete timelapse"
                      aria-label="Delete timelapse"
                      @click="deleteArtifact(session, session.products.timelapse)"
                    >
                      <TrashIcon class="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
              <div class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300">
                <div class="text-xs uppercase tracking-wide text-gray-500">Keogram</div>
                <div class="mt-2 flex items-start justify-between gap-3">
                  <div>{{ describeArtifact(session.products?.keogram) }}</div>
                  <div v-if="session.products?.keogram" class="flex items-center gap-2">
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
                      title="Download keogram"
                      aria-label="Download keogram"
                      @click="downloadRelativePath(session.products.keogram.relativePath)"
                    >
                      <ArrowDownTrayIcon class="h-4 w-4" />
                    </button>
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                      :disabled="cleanupBusy || status?.generateInProgress || session.id === currentSession?.id"
                      title="Delete keogram"
                      aria-label="Delete keogram"
                      @click="deleteArtifact(session, session.products.keogram)"
                    >
                      <TrashIcon class="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
              <div class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300">
                <div class="text-xs uppercase tracking-wide text-gray-500">Startrails</div>
                <div class="mt-2 flex items-start justify-between gap-3">
                  <div>{{ describeArtifact(session.products?.startrails) }}</div>
                  <div v-if="session.products?.startrails" class="flex items-center gap-2">
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-cyan-500/40 bg-cyan-500/10 text-cyan-100 transition hover:bg-cyan-500/20"
                      title="Download startrails"
                      aria-label="Download startrails"
                      @click="downloadRelativePath(session.products.startrails.relativePath)"
                    >
                      <ArrowDownTrayIcon class="h-4 w-4" />
                    </button>
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                      :disabled="cleanupBusy || status?.generateInProgress || session.id === currentSession?.id"
                      title="Delete startrails"
                      aria-label="Delete startrails"
                      @click="deleteArtifact(session, session.products.startrails)"
                    >
                      <TrashIcon class="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <div v-if="isSessionDetailsOpen(session.id)" class="mt-4 rounded-2xl border border-gray-700 bg-gray-800/50 p-4">
              <div class="mb-3 flex items-center justify-between gap-3">
                <div>
                  <div class="text-sm font-semibold text-white">Stored Frames</div>
                  <div class="text-xs text-gray-400">
                    {{ formatCount(sessionDetails(session.id)?.frames?.length ?? session.storedFrameCount ?? 0) }}
                    retained frame(s)
                  </div>
                </div>
                <button
                  class="rounded-lg border border-gray-600 bg-gray-900/70 px-3 py-2 text-xs text-gray-200 transition hover:border-cyan-400"
                  @click="refreshSessionDetails(session.id)"
                >
                  Refresh Files
                </button>
              </div>

              <div v-if="sessionDetailsLoading(session.id)" class="rounded-xl border border-dashed border-gray-700 bg-gray-900/40 px-4 py-6 text-center text-sm text-gray-500">
                Loading stored files…
              </div>
              <div
                v-else-if="!sessionDetails(session.id)?.frames?.length"
                class="rounded-xl border border-dashed border-gray-700 bg-gray-900/40 px-4 py-6 text-center text-sm text-gray-500"
              >
                No stored frames are available for this session.
              </div>
              <div v-else class="max-h-80 space-y-2 overflow-y-auto pr-1">
                <div
                  v-for="frame in sessionDetails(session.id).frames"
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
                      :title="`Download ${frame.name}`"
                      :aria-label="`Download ${frame.name}`"
                      @click="downloadRelativePath(frame.relativePath, frame.name)"
                    >
                      <ArrowDownTrayIcon class="h-4 w-4" />
                    </button>
                    <button
                      class="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-rose-500/40 bg-rose-500/10 text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                      :disabled="cleanupBusy || status?.generateInProgress || session.id === currentSession?.id"
                      :title="`Delete ${frame.name}`"
                      :aria-label="`Delete ${frame.name}`"
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
import { computed, onBeforeUnmount, onMounted, reactive } from 'vue';
import { storeToRefs } from 'pinia';
import {
  ArrowDownTrayIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  TrashIcon,
} from '@heroicons/vue/24/outline';
import toggleButton from '@/components/helpers/toggleButton.vue';
import { usePinsAllSkyStore } from '../store/pinsAllskyStore';

const store = usePinsAllSkyStore();
const {
  status,
  config,
  error,
  loading,
  saving,
  cleanupBusy,
  actionMessage,
  currentImageUrl,
  sessionDetailsById,
  detailsLoadingById,
} = storeToRefs(store);

const sectionOpen = reactive({
  overview: false,
  captureSettings: false,
  recentSessions: false,
  dependencies: false,
});

const sessionDetailOpen = reactive({});
const estimateWindow = reactive({
  startLocal: '',
  endLocal: '',
});

const currentSession = computed(() => status.value?.currentSession || null);
const recentSessions = computed(() => status.value?.recentSessions || []);
const backendError = computed(() => status.value?.lastError || null);
const storage = computed(() => {
  const raw = status.value?.storage || {};
  const diskAvailableBytes = Number(raw.diskAvailableBytes || 0);
  const diskTotalBytes = Number(raw.diskTotalBytes || 0);

  return {
    ...raw,
    diskUsedBytes: Number(raw.diskUsedBytes ?? Math.max(0, diskTotalBytes - diskAvailableBytes)),
  };
});
const estimateBaseline = computed(() => status.value?.estimateBaseline || null);
const estimateBaselineLabel = computed(() => {
  if (!estimateBaseline.value) {
    return 'No baseline session yet';
  }

  return estimateBaseline.value.label || estimateBaseline.value.sessionId;
});
const estimateBaselineAverageFrameBytes = computed(() => estimateBaseline.value?.averageFrameBytes || 0);

const dependencyRows = computed(() => [
  {
    label: 'rpicam-still',
    ready: Boolean(status.value?.dependencies?.rpiCamStillAvailable),
  },
  {
    label: 'ffmpeg',
    ready: Boolean(status.value?.dependencies?.ffmpegAvailable),
  },
  {
    label: 'AllSky keogram',
    ready: Boolean(status.value?.dependencies?.keogramAvailable),
  },
  {
    label: 'AllSky startrails',
    ready: Boolean(status.value?.dependencies?.startrailsAvailable),
  },
]);

const missingDependencies = computed(() =>
  dependencyRows.value.filter((item) => !item.ready).map((item) => item.label)
);

const parsedEstimateStart = computed(() => parseLocalInputValue(estimateWindow.startLocal));
const parsedEstimateEnd = computed(() => parseLocalInputValue(estimateWindow.endLocal));
const estimateDurationSeconds = computed(() => {
  if (!parsedEstimateStart.value || !parsedEstimateEnd.value) {
    return 0;
  }

  return Math.max(0, Math.round((parsedEstimateEnd.value.getTime() - parsedEstimateStart.value.getTime()) / 1000));
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
  if (!config.value?.products?.timelapseEnabled || estimatedFrameCount.value <= 0) {
    return 0;
  }

  const fps = Math.max(1, Number(config.value?.products?.timelapseFps || 1));
  const bitrateKbps = Math.max(1000, Number(config.value?.products?.timelapseBitrateKbps || 1000));
  const videoSeconds = estimatedFrameCount.value / fps;
  return Math.round(videoSeconds * bitrateKbps * 1000 / 8);
});
const estimatedKeogramBytes = computed(() =>
  config.value?.products?.keogramEnabled ? Number(estimateBaseline.value?.keogramBytes || 0) : 0
);
const estimatedStartrailsBytes = computed(() =>
  config.value?.products?.startrailsEnabled ? Number(estimateBaseline.value?.startrailsBytes || 0) : 0
);
const estimatedStorageBytes = computed(() =>
  estimatedFrameStorageBytes.value
  + estimatedTimelapseBytes.value
  + estimatedKeogramBytes.value
  + estimatedStartrailsBytes.value
);
const estimateDurationLabel = computed(() => formatDuration(estimateDurationSeconds.value));
const estimateExceedsAvailable = computed(() => {
  if (estimatedStorageBytes.value <= 0) {
    return false;
  }

  const diskAvailableBytes = Number(storage.value.diskAvailableBytes || 0);
  if (diskAvailableBytes > 0 && estimatedStorageBytes.value > diskAvailableBytes) {
    return true;
  }

  if (storage.value.limitEnabled) {
    const pluginAvailableBytes = Number(storage.value.pluginAvailableBytes || 0);
    return estimatedStorageBytes.value > pluginAvailableBytes;
  }

  return false;
});
const estimateWarning = computed(() => {
  if (!parsedEstimateStart.value || !parsedEstimateEnd.value) {
    return 'Set a valid start and end time to estimate storage.';
  }

  if (estimateDurationSeconds.value <= 0) {
    return 'End time must be later than start time.';
  }

  if (!estimateBaseline.value) {
    return 'No completed baseline session is available yet. Finish one session first to estimate storage reliably.';
  }

  if (estimatedStorageBytes.value <= 0) {
    return null;
  }

  const warnings = [];
  const diskAvailableBytes = Number(storage.value.diskAvailableBytes || 0);
  if (diskAvailableBytes > 0 && estimatedStorageBytes.value > diskAvailableBytes) {
    warnings.push(`Pi free space (${formatSize(diskAvailableBytes)})`);
  }

  if (storage.value.limitEnabled) {
    const pluginAvailableBytes = Number(storage.value.pluginAvailableBytes || 0);
    if (estimatedStorageBytes.value > pluginAvailableBytes) {
      warnings.push(`plugin limit remaining (${formatSize(pluginAvailableBytes)})`);
    }
  }

  if (warnings.length === 0) {
    return null;
  }

  return `Expected storage ${formatSize(estimatedStorageBytes.value)} exceeds ${warnings.join(' and ')}.`;
});

const meteringOptions = [
  { value: 'centre', label: 'Centre' },
  { value: 'spot', label: 'Spot' },
  { value: 'average', label: 'Average' },
  { value: 'custom', label: 'Custom' },
];

const awbOptions = [
  { value: 'auto', label: 'Auto' },
  { value: 'incandescent', label: 'Incandescent' },
  { value: 'tungsten', label: 'Tungsten' },
  { value: 'fluorescent', label: 'Fluorescent' },
  { value: 'indoor', label: 'Indoor' },
  { value: 'daylight', label: 'Daylight' },
  { value: 'cloudy', label: 'Cloudy' },
  { value: 'custom', label: 'Custom' },
];

const denoiseOptions = [
  { value: 'auto', label: 'Auto' },
  { value: 'off', label: 'Off' },
  { value: 'cdn_off', label: 'CDN Off' },
  { value: 'cdn_fast', label: 'CDN Fast' },
  { value: 'cdn_hq', label: 'CDN High Quality' },
];

const toggleSection = (sectionKey) => {
  sectionOpen[sectionKey] = !sectionOpen[sectionKey];
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
  await store.startSession();
};

const stopSession = async () => {
  await store.stopSession(true);
};

const generateArtifacts = async (sessionId = null) => {
  await store.generateArtifacts(sessionId);
};

const deleteSession = async (session) => {
  const sessionName = session?.label || session?.id || 'this session';
  if (!window.confirm(`Delete ${sessionName} and all of its captured frames/products?`)) {
    return;
  }

  await store.deleteSession(session.id);
};

const deleteAllSessions = async () => {
  if (!window.confirm('Delete all stored AllSky sessions, frames, and generated products?')) {
    return;
  }

  await store.deleteAllSessions();
};

const deleteArtifact = async (session, artifact) => {
  if (!artifact?.relativePath) {
    return;
  }

  const artifactName = artifact.name || artifact.relativePath.split('/').pop() || 'this artifact';
  if (!window.confirm(`Delete ${artifactName}?`)) {
    return;
  }

  await store.deleteArtifact(session.id, artifact.relativePath);
};

const deleteFrame = async (session, frame) => {
  if (!frame?.relativePath) {
    return;
  }

  const frameName = frame.name || frame.relativePath.split('/').pop() || 'this frame';
  if (!window.confirm(`Delete ${frameName}?`)) {
    return;
  }

  await store.deleteFrame(session.id, frame.relativePath);
};

const downloadRelativePath = async (relativePath, fallbackName = null) => {
  const derivedName = fallbackName || relativePath?.split('/').pop() || 'download';
  await store.downloadFile(relativePath, derivedName);
};

const sessionDetails = (sessionId) => sessionDetailsById.value?.[sessionId] || null;
const sessionDetailsLoading = (sessionId) => Boolean(detailsLoadingById.value?.[sessionId]);
const isSessionDetailsOpen = (sessionId) => Boolean(sessionDetailOpen[sessionId]);
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

  if (typeof value === 'string' || typeof value === 'number') {
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  if (typeof value === 'object') {
    const utcDateTime = typeof value.utcDateTime === 'string' ? value.utcDateTime : null;
    if (utcDateTime) {
      const parsedUtc = new Date(
        /[zZ]|[+-]\d{2}:\d{2}$/.test(utcDateTime) ? utcDateTime : `${utcDateTime}Z`
      );
      if (!Number.isNaN(parsedUtc.getTime())) {
        return parsedUtc;
      }
    }

    const localDateTime = typeof value.localDateTime === 'string' ? value.localDateTime : null;
    if (localDateTime) {
      const parsedLocal = new Date(localDateTime);
      if (!Number.isNaN(parsedLocal.getTime())) {
        return parsedLocal;
      }
    }

    const dateTime = typeof value.dateTime === 'string' ? value.dateTime : null;
    if (dateTime) {
      const parsedDateTime = new Date(dateTime);
      if (!Number.isNaN(parsedDateTime.getTime())) {
        return parsedDateTime;
      }
    }
  }

  return null;
};

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
    return '—';
  }

  return parsed.toLocaleString();
};

const formatInterval = (seconds) => {
  if (!Number.isFinite(seconds)) {
    return '—';
  }

  return `${seconds}s`;
};

const formatCount = (value) => {
  if (!Number.isFinite(value)) {
    return '—';
  }

  return new Intl.NumberFormat().format(value);
};

const formatDuration = (seconds) => {
  if (!Number.isFinite(seconds) || seconds <= 0) {
    return '—';
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
};

const describeArtifact = (artifact) => {
  if (!artifact) {
    return 'Not generated';
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
