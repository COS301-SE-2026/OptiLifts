import styles from './page-title.module.css';

interface PageTitleProps {
  readonly title: string;
}

export function PageTitle({ title }: PageTitleProps) {
  return (
    <div className={styles.wrapper}>
      <span className={styles.bar} />
      <span className={styles.title}>{title}</span>
    </div>
  );
}
