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

        <div v-if="sectionOpen.overview" class="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
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
          <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <div class="rounded-xl border border-gray-700 bg-gray-900/50 p-3">
              <div class="text-xs uppercase tracking-wide text-gray-500">Plugin Used</div>
              <div class="mt-2 text-sm text-white">{{ formatSize(storage.pluginUsedBytes) }}</div>
            </div>
            <div class="rounded-xl border border-gray-700 bg-gray-900/50 p-3">
              <div class="text-xs uppercase tracking-wide text-gray-500">Plugin Limit</div>
              <div class="mt-2 text-sm text-white">
                {{ storage.limitEnabled ? formatSize(storage.maxPluginUsageBytes) : 'Unlimited' }}
              </div>
            </div>
            <div class="rounded-xl border border-gray-700 bg-gray-900/50 p-3">
              <div class="text-xs uppercase tracking-wide text-gray-500">Plugin Available</div>
              <div class="mt-2 text-sm text-white">
                {{ storage.limitEnabled ? formatSize(storage.pluginAvailableBytes) : 'Unlimited' }}
              </div>
            </div>
            <div class="rounded-xl border border-gray-700 bg-gray-900/50 p-3">
              <div class="text-xs uppercase tracking-wide text-gray-500">Disk Available</div>
              <div class="mt-2 text-sm text-white">{{ formatSize(storage.diskAvailableBytes) }}</div>
            </div>
          </div>

          <div class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-gray-700 bg-gray-900/50 px-4 py-3">
            <div class="text-sm text-gray-300">
              <span :class="storage.withinLimit ? 'text-emerald-300' : 'text-amber-200'">
                {{ storage.withinLimit || !storage.limitEnabled ? 'Storage within limit.' : 'Storage limit exceeded.' }}
              </span>
              <span v-if="storage.limitEnabled" class="text-gray-500">
                Oldest completed sessions are pruned automatically when needed.
              </span>
            </div>
            <button
              class="rounded-lg border border-rose-500/40 bg-rose-500/10 px-3 py-2 text-sm font-medium text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
              :disabled="cleanupBusy || status?.captureRunning || status?.generateInProgress || recentSessions.length === 0"
              @click.stop="deleteAllSessions"
            >
              {{ cleanupBusy ? 'Cleaning…' : 'Delete All' }}
            </button>
          </div>

          <div
            v-if="cleanupSummary"
            class="rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-100"
          >
            {{ cleanupSummary }}
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
                  class="rounded-lg border border-rose-500/40 bg-rose-500/10 px-3 py-2 text-sm font-medium text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-40"
                  :disabled="cleanupBusy || status?.captureRunning || status?.generateInProgress || session.id === currentSession?.id"
                  @click="deleteSession(session)"
                >
                  Delete
                </button>
                <a
                  v-if="session.products?.timelapse"
                  class="rounded-lg border border-gray-600 bg-gray-800 px-3 py-2 text-sm text-white transition hover:border-cyan-400"
                  :href="artifactUrl(session.products.timelapse.relativePath)"
                  target="_blank"
                  rel="noreferrer"
                >
                  Timelapse
                </a>
                <a
                  v-if="session.products?.keogram"
                  class="rounded-lg border border-gray-600 bg-gray-800 px-3 py-2 text-sm text-white transition hover:border-cyan-400"
                  :href="artifactUrl(session.products.keogram.relativePath)"
                  target="_blank"
                  rel="noreferrer"
                >
                  Keogram
                </a>
                <a
                  v-if="session.products?.startrails"
                  class="rounded-lg border border-gray-600 bg-gray-800 px-3 py-2 text-sm text-white transition hover:border-cyan-400"
                  :href="artifactUrl(session.products.startrails.relativePath)"
                  target="_blank"
                  rel="noreferrer"
                >
                  Startrails
                </a>
              </div>
            </div>

            <div class="mt-4 grid gap-3 sm:grid-cols-3">
              <div class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300">
                <div class="text-xs uppercase tracking-wide text-gray-500">Timelapse</div>
                <div class="mt-2">
                  {{ describeArtifact(session.products?.timelapse) }}
                </div>
              </div>
              <div class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300">
                <div class="text-xs uppercase tracking-wide text-gray-500">Keogram</div>
                <div class="mt-2">
                  {{ describeArtifact(session.products?.keogram) }}
                </div>
              </div>
              <div class="rounded-xl border border-gray-700 bg-gray-800/60 p-3 text-sm text-gray-300">
                <div class="text-xs uppercase tracking-wide text-gray-500">Startrails</div>
                <div class="mt-2">
                  {{ describeArtifact(session.products?.startrails) }}
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
import toggleButton from '@/components/helpers/toggleButton.vue';
import { usePinsAllSkyStore } from '../store/pinsAllskyStore';

const store = usePinsAllSkyStore();
const { status, config, error, loading, saving, cleanupBusy, cleanupResult, currentImageUrl } = storeToRefs(store);

const currentSession = computed(() => status.value?.currentSession || null);
const recentSessions = computed(() => status.value?.recentSessions || []);
const backendError = computed(() => status.value?.lastError || null);
const storage = computed(() => status.value?.storage || {});
const cleanupSummary = computed(() => {
  if (!cleanupResult.value) {
    return null;
  }

  const deletedSessionCount = cleanupResult.value.deletedSessionCount || 0;
  const freedBytes = cleanupResult.value.freedBytes || 0;
  const remainingUsed = cleanupResult.value.storage?.pluginUsedBytes ?? 0;

  if (deletedSessionCount === 0) {
    return 'No sessions were deleted.';
  }

  return `Deleted ${deletedSessionCount} session${deletedSessionCount === 1 ? '' : 's'}, freed ${formatSize(freedBytes)}, remaining plugin usage ${formatSize(remainingUsed)}.`;
});

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

const sectionOpen = reactive({
  overview: false,
  captureSettings: false,
  recentSessions: false,
  dependencies: false,
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

const artifactUrl = (relativePath) => store.artifactUrl(relativePath);

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
  await refreshAll();
  store.startPolling();
});

onBeforeUnmount(() => {
  store.stopPolling();
});
</script>
