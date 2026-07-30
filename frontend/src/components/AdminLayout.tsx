import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { 
    LayoutDashboard, Film, Calendar, Building, 
    Users, ShieldAlert, Percent, ScanQrCode, LogOut, Globe 
} from 'lucide-react';

interface AdminLayoutProps {
    children: React.ReactNode;
}

export const AdminLayout: React.FC<AdminLayoutProps> = ({ children }) => {
    const { user, logout } = useAuthStore();
    const navigate = useNavigate();

    const roles = user?.roles || [];
    const isSuperAdmin = roles.includes('SuperAdmin');
    const isManager = roles.includes('CinemaManager');
    const isCashier = roles.includes('Cashier');

    const handleLogout = () => {
        logout();
        navigate('/');
    };

    const menuItems = [
        { path: '/admin/dashboard', label: 'Статистика', icon: LayoutDashboard, show: isSuperAdmin },
        { path: '/admin/movies', label: 'Фільми', icon: Film, show: isSuperAdmin || isManager },
        { path: '/admin/sessions', label: 'Розклад сеансів', icon: Calendar, show: isSuperAdmin || isManager },
        { path: '/admin/halls', label: 'Кінозали', icon: Building, show: isSuperAdmin || isManager },
        { path: '/admin/promocodes', label: 'Промокоди', icon: Percent, show: isSuperAdmin },
        { path: '/admin/staff', label: 'Персонал', icon: Users, show: isSuperAdmin },
        { path: '/admin/cinemas', label: 'Кінотеатри', icon: Building, show: isSuperAdmin },
        { path: '/admin/audit', label: 'Аудит логів', icon: ShieldAlert, show: isSuperAdmin },
        { path: '/cashier/scan', label: 'Сканер квитків', icon: ScanQrCode, show: isSuperAdmin || isCashier || isManager },
    ];

    return (
        <div className="flex min-h-screen bg-[#0f111a] text-white">
            <aside className="w-64 bg-[#141622] border-r border-white/5 flex flex-col justify-between shrink-0 sticky top-0 h-screen">
                <div className="p-6">
                    <div className="flex items-center gap-2 mb-8 select-none">
                        <span className="text-[#ffbd14] text-xl font-black tracking-wider">StarPlex</span>
                        <span className="text-[9px] uppercase font-bold bg-white/10 px-2 py-0.5 rounded text-gray-400">Panel</span>
                    </div>

                    <nav className="space-y-1.5">
                        {menuItems.map((item) => {
                            if (!item.show) return null;
                            const Icon = item.icon;
                            return (
                                <NavLink
                                    key={item.path}
                                    to={item.path}
                                    className={({ isActive }) => `
                                        flex items-center gap-3 px-4 py-3 rounded-xl text-xs font-bold uppercase tracking-wider transition-all border-none
                                        ${isActive 
                                            ? 'bg-[#ffbd14] text-black shadow-lg shadow-[#ffbd14]/15' 
                                            : 'text-gray-400 hover:bg-white/5 hover:text-white'
                                        }
                                    `}
                                >
                                    <Icon className="w-4 h-4 shrink-0" />
                                    <span>{item.label}</span>
                                </NavLink>
                            );
                        })}
                    </nav>
                </div>

                <div className="p-4 border-t border-white/5 bg-[#11131c] flex flex-col gap-3">
                    <button 
                        onClick={() => navigate('/')}
                        className="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-white/5 hover:bg-[#ffbd14]/10 text-gray-300 hover:text-[#ffbd14] rounded-xl font-bold text-[11px] uppercase tracking-wider transition-all cursor-pointer border-none"
                    >
                        <Globe className="w-3.5 h-3.5" />
                        <span>На головну сайту</span>
                    </button>

                    <div className="flex items-center justify-between gap-2 border-t border-white/5 pt-3">
                        <div className="min-w-0">
                            <p className="text-xs font-black truncate">{user?.email}</p>
                            <p className="text-[10px] text-gray-500 font-bold uppercase mt-0.5">{roles[0]}</p>
                        </div>
                        <button 
                            onClick={handleLogout}
                            className="p-2 bg-white/5 hover:bg-red-500/10 text-gray-400 hover:text-red-400 rounded-xl transition-colors cursor-pointer border-none"
                            title="Вийти з системи"
                        >
                            <LogOut className="w-4 h-4" />
                        </button>
                    </div>
                </div>
            </aside>

            <main className="flex-grow p-8 lg:p-10 overflow-y-auto">
                <div className="max-w-7xl mx-auto">
                    {children}
                </div>
            </main>
        </div>
    );
};