import type { ReactNode } from 'react';

interface PageLayoutProps {
  title: string;
  action?: ReactNode;
  children: ReactNode;
}

export function PageLayout({ title, action, children }: PageLayoutProps) {
  return (
    <div className="flex-1 p-6 min-h-screen bg-[#F1F5F9]">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-xl font-bold text-[#0F172A]">{title}</h1>
        {action && <div>{action}</div>}
      </div>
      {children}
    </div>
  );
}
