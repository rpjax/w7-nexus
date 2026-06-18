import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

export type PageHeadingBackLink = {
  to: string;
  label: string;
};

type PageHeadingProps = {
  title: string;
  kicker?: string;
  subtitle?: ReactNode;
  backLink?: PageHeadingBackLink;
  kickerVariant?: 'default' | 'admin';
  className?: string;
};

export function PageHeading({
  title,
  kicker,
  subtitle,
  backLink,
  kickerVariant = 'default',
  className = '',
}: PageHeadingProps) {
  const kickerClass = kickerVariant === 'admin'
    ? 'page-heading__kicker page-heading__kicker--admin'
    : 'page-heading__kicker';

  return (
    <header className={`page-heading ${className}`.trim()}>
      {backLink ? (
        <p className="page-heading__back muted small">
          <Link to={backLink.to}>← {backLink.label}</Link>
        </p>
      ) : null}
      <div className="page-heading__main">
        {kicker ? <p className={kickerClass}>{kicker}</p> : null}
        <h1 className="page-heading__title">{title}</h1>
        {subtitle ? (
          <p className="page-heading__subtitle muted">{subtitle}</p>
        ) : null}
      </div>
    </header>
  );
}
