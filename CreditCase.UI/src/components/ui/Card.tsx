import type { ReactNode } from 'react';

interface CardProps {
  children: ReactNode;
  className?: string;
}

export function Card({ children, className = '' }: CardProps) {
  return (
    <div className={`bg-white rounded-xl border border-[#E2E8F0] shadow-sm ${className}`}>
      {children}
    </div>
  );
}

interface StatCardProps {
  label: string;
  value: string | number;
  sub?: string;
  accent?: 'default' | 'danger' | 'success' | 'warning';
}

const ACCENT_CLASSES = {
  default: 'text-[#1B4FD8]',
  danger:  'text-red-600',
  success: 'text-green-600',
  warning: 'text-amber-600',
};

export function StatCard({ label, value, sub, accent = 'default' }: StatCardProps) {
  return (
    <Card className="p-5">
      <p className="text-sm text-[#64748B] font-medium">{label}</p>
      <p className={`text-2xl font-bold mt-1 ${ACCENT_CLASSES[accent]}`}>{value}</p>
      {sub && <p className="text-xs text-[#64748B] mt-0.5">{sub}</p>}
    </Card>
  );
}
