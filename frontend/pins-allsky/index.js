import { markRaw } from 'vue';
import PinsAllSkyIcon from './components/PinsAllSkyIcon.vue';
import PinsAllSkyView from './views/PinsAllSkyView.vue';
import i18n from '@/i18n';
import { usePluginStore } from '@/store/pluginStore';
import metadata from './plugin.json';
import messages from './locales/messages';

const SUPPORTED_LOCALES = ['en', 'de', 'fr', 'it', 'cz', 'cn', 'pt', 'es', 'pl', 'nl'];

export default {
  metadata,
  install(app, options) {
    const pluginStore = usePluginStore();
    const router = options.router;

    for (const locale of SUPPORTED_LOCALES) {
      i18n.global.mergeLocaleMessage(locale, {
        plugins: {
          pinsAllSky: messages,
        },
      });
    }

    const currentPlugin = pluginStore.plugins.find((plugin) => plugin.id === metadata.id);
    const pluginPath = currentPlugin?.pluginPath || '/plugin1';

    router.addRoute({
      path: pluginPath,
      component: PinsAllSkyView,
      meta: { requiresSetup: true },
    });

    if (currentPlugin?.enabled) {
      pluginStore.addNavigationItem({
        pluginId: metadata.id,
        path: pluginPath,
        icon: markRaw(PinsAllSkyIcon),
        title: i18n.global.t('plugins.pinsAllSky.title'),
      });
    }
  },
};
