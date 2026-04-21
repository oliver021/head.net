import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  tutorialSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'getting-started/installation',
        'getting-started/configuration',
        'getting-started/quick-example',
        'getting-started/development-philosophy',
      ],
    },
    {
      type: 'category',
      label: 'Guides',
      items: [
        'guides/entity-lifecycle-hooks',
        'guides/custom-actions',
        'guides/query-and-paging',
        'guides/authorization-and-ownership',
        'guides/setup-classes',
      ],
    },
  ],
};

export default sidebars;
