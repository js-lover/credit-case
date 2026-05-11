import { InstallmentStatus, INSTALLMENT_STATUS_LABELS, LoanStatus, LOAN_STATUS_LABELS } from '../../types';

interface InstallmentBadgeProps {
  status: InstallmentStatus;
}

const INSTALLMENT_COLORS: Record<InstallmentStatus, string> = {
  [InstallmentStatus.Paid]:    'bg-green-100 text-green-700',
  [InstallmentStatus.Unpaid]:  'bg-amber-100 text-amber-700',
  [InstallmentStatus.Overdue]: 'bg-red-100 text-red-700',
};

export function InstallmentBadge({ status }: InstallmentBadgeProps) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${INSTALLMENT_COLORS[status]}`}>
      {INSTALLMENT_STATUS_LABELS[status]}
    </span>
  );
}

interface LoanStatusBadgeProps {
  status: LoanStatus;
}

const LOAN_COLORS: Record<LoanStatus, string> = {
  [LoanStatus.Active]: 'bg-blue-100 text-blue-700',
  [LoanStatus.Closed]: 'bg-gray-100 text-gray-600',
};

export function LoanStatusBadge({ status }: LoanStatusBadgeProps) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${LOAN_COLORS[status]}`}>
      {LOAN_STATUS_LABELS[status]}
    </span>
  );
}
