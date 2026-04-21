import type {ReactNode} from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';

import styles from './index.module.css';

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();

  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link className="button button--primary button--lg" to="/docs/getting-started/quick-example">
            Quick Example
          </Link>
          <Link className="button button--secondary button--lg" style={{marginLeft: '1rem'}} to="/docs">
            Read the Docs
          </Link>
        </div>
      </div>
    </header>
  );
}

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();

  return (
    <Layout
      title={siteConfig.title}
      description="Head.Net — the EF Core SDK that collapses the CRUD tax on .NET APIs.">
      <HomepageHeader />
      <main>
        <section className="container margin-top--lg margin-bottom--xl">
          <div className="row">
            <div className="col col--4">
              <h2>Less boilerplate</h2>
              <p>
                Declare the entity, describe the surface, and let Head.Net generate
                the Minimal API endpoints, EF Core wiring, and OpenAPI metadata.
              </p>
            </div>
            <div className="col col--4">
              <h2>Hooks as first class</h2>
              <p>
                BeforeCreate, AfterUpdate, BeforeDelete — lifecycle hooks run at the
                right moment without leaking HTTP concerns into your business logic.
              </p>
            </div>
            <div className="col col--4">
              <h2>Domain actions, not workarounds</h2>
              <p>
                Custom actions like <code>pay</code> or <code>archive</code> route
                predictably to <code>/invoices/&#123;id&#125;/pay</code> and appear
                in OpenAPI automatically.
              </p>
            </div>
          </div>
        </section>
      </main>
    </Layout>
  );
}
