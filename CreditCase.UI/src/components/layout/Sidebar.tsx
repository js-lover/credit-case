import { NavLink } from 'react-router-dom';

const NAV_ITEMS = [
  { to: '/',            label: 'Dashboard',  icon: '⊞' },
  { to: '/customers',   label: 'Müşteriler', icon: '👤' },
  { to: '/loans',       label: 'Krediler',   icon: '💳' },
  { to: '/payments',    label: 'Ödemeler',   icon: '✓' },
];

export function Sidebar() {
  return (
    <aside className="w-56 shrink-0 bg-[#0F172A] min-h-screen flex flex-col">
      {/* Logo */}
      <div className="px-4 py-4 border-b border-white/10">
        <img src="/logo.svg" alt="Architecht" className="h-20 w-auto" />
      </div>

      {/* Nav */}
      <nav className="flex-1 px-3 py-4 space-y-1">
        {NAV_ITEMS.map(({ to, label, icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors
               ${isActive
                 ? 'bg-[#1B4FD8] text-white'
                 : 'text-white/60 hover:bg-white/10 hover:text-white'}`
            }
          >
            <span className="text-base">{icon}</span>
            {label}
          </NavLink>
        ))}
      </nav>

      <div className="px-5 py-4 border-t border-white/10">
        <p className="text-white/30 text-xs">v1.0.0</p>
      </div>
    </aside>
  );
}
