import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Head.Net Docs',
  tagline: 'Convention-first EF Core and Minimal API docs for Head.Net',
  favicon: 'img/favicon.ico',
  url: 'https://headnet.example.com',
  baseUrl: '/',
  organizationName: 'oliver021',
  projectName: 'Head.Net',
  onBrokenLinks: 'throw',
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },
  plugins: [
    [
      'docusaurus-plugin-openapi-docs',
      {
        id: 'openapi',
        docsPluginId: 'classic',
        config: {
          sampleApi: {
            specPath: 'static/openapi.yaml',
            outputDir: 'docs/api',
            sidebarOptions: { groupPathsBy: 'tag' },
          },
        },
      },
    ],
  ],
  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          docItemComponent: '@theme/ApiItem',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],
  themes: ['docusaurus-theme-openapi-docs'],
  themeConfig: {
    image: 'img/docusaurus-social-card.jpg',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Head.Net',
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/oliver021/entity-dock',
          label: 'EntityDock Origin',
          position: 'right',
        },
        {
          href: 'https://github.com/oliver021/Head.Net',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'light',
      links: [
        {
          title: 'Getting Started',
          items: [
            { label: 'Installation', to: '/docs/getting-started/installation' },
            { label: 'Quick Example', to: '/docs/getting-started/quick-example' },
            { label: 'Philosophy', to: '/docs/getting-started/development-philosophy' },
          ],
        },
        {
          title: 'Guides',
          items: [
            { label: 'Lifecycle Hooks', to: '/docs/guides/entity-lifecycle-hooks' },
            { label: 'Custom Actions', to: '/docs/guides/custom-actions' },
            { label: 'Authorization', to: '/docs/guides/authorization-and-ownership' },
            { label: 'Setup Classes', to: '/docs/guides/setup-classes' },
          ],
        },
        {
          title: 'Project',
          items: [
            { label: 'GitHub', href: 'https://github.com/oliver021/Head.Net' },
          ],
        },
      ],
      copyright: `Copyright ${String.fromCharCode(169)} ${new Date().getFullYear()} Head.Net. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'bash', 'json', 'xml-doc'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
