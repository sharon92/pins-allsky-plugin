import { markRaw } from 'vue';
import PinsAllSkyIcon from './components/PinsAllSkyIcon.vue';
import PinsAllSkyView from './views/PinsAllSkyView.vue';
import { usePluginStore } from '@/store/pluginStore';
import metadata from './plugin.json';

export default {
  metadata,
  install(app, options) {
    const pluginStore = usePluginStore();
    const router = options.router;

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
        title: metadata.name,
      });
    }
  },
};
