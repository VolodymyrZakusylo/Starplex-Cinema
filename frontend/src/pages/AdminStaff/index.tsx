import React, { useEffect, useState } from 'react';
import { Shield, Search, SlidersHorizontal, Edit2, MapPin, ChevronLeft, ChevronRight } from 'lucide-react';
import { usersApi, type GetStaffParams } from '@/api/users';
import { cinemasApi } from '@/api/cinemas';
import type { UserStaffDto } from '@/types/admin';
import type { CinemaDto } from '@/types/cinemas';
import { useToast } from '@/hooks/useToast';
import { StaffRoleModal } from './components/StaffRoleModal';

const RoleLabels: Record<string, string> = {
    'SuperAdmin': 'Глобальний Адмін',
    'CinemaManager': 'Менеджер філії',
    'Cashier': 'Касир',
    'Customer': 'Клієнт'
};

export const AdminStaffPage: React.FC = () => {
    const { showError, showSuccess } = useToast();
    const [users, setUsers] = useState<UserStaffDto[]>([]);
    const [cinemas, setCinemas] = useState<CinemaDto[]>([]);
    const [totalCount, setTotalCount] = useState<number>(0);

    const [searchTerm, setSearchTerm] = useState<string>('');
    const [roleFilter, setRoleFilter] = useState<string>('');
    const [cinemaFilter, setCinemaFilter] = useState<string>('');
    const [currentPage, setCurrentPage] = useState<number>(1);
    const pageSize = 10;

    const [isLoading, setIsLoading] = useState<boolean>(true);

    const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
    const [selectedUser, setSelectedUser] = useState<UserStaffDto | null>(null);

    const fetchCinemas = async () => {
        try {
            const data = await cinemasApi.getAll();
            setCinemas(data);
        } catch (err) {
            showError('Не вдалося завантажити список кінотеатрів мережі.');
        }
    };

    const fetchUsers = async () => {
        setIsLoading(true);
        try {
            const params: GetStaffParams = {
                page: currentPage,
                pageSize: pageSize
            };
            if (searchTerm.trim()) params.searchTerm = searchTerm.trim();
            if (roleFilter) params.roleFilter = roleFilter;
            if (cinemaFilter) params.cinemaIdFilter = cinemaFilter;

            const response = await usersApi.getStaff(params);
            setUsers(response.users);
            setTotalCount(response.totalCount);
        } catch (err: any) {
            showError(err.response?.data?.message || 'Не вдалося завантажити реєстр користувачів.');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchCinemas();
    }, []);

    useEffect(() => {
        fetchUsers();
    }, [currentPage, roleFilter, cinemaFilter]);

    const handleSearchSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        setCurrentPage(1);
        fetchUsers();
    };

    const handleOpenEditModal = (user: UserStaffDto) => {
        setSelectedUser(user);
        setIsModalOpen(true);
    };

    const handleSaveRoleUpdate = async (userId: string, newRoleName: string, cinemaId: string | null) => {
        await usersApi.updateRole(userId, newRoleName, cinemaId);
        showSuccess('Права та рівень доступу користувача успішно змінено.');
        fetchUsers();
    };

    const totalPages = Math.ceil(totalCount / pageSize);

    return (
        <div className="w-full select-none">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-6 mb-10 border-b border-white/5 pb-6">
                <div>
                    <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
                        <Shield className="text-[#ffbd14] w-8 h-8" /> Персонал та Права
                    </h1>
                    <p className="text-gray-400 text-sm mt-1">Призначення ролей адміністраторам мережі, менеджерам філій та касирам</p>
                </div>
            </div>

            <div className="bg-[#1a1c26] border border-white/5 rounded-2xl p-4 sm:p-6 mb-8 shadow-xl">
                <form onSubmit={handleSearchSubmit} className="flex flex-col lg:flex-row gap-4">
                    <div className="relative flex-1">
                        <input
                            type="text"
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            placeholder="Шукати за Email, ім'ям або прізвищем..."
                            className="w-full bg-[#111219] border border-white/10 rounded-xl pl-10 pr-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14]"
                        />
                        <Search className="w-4 h-4 text-gray-500 absolute left-3.5 top-3.5" />
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 lg:w-[450px]">
                        <select
                            value={roleFilter}
                            onChange={(e) => { setRoleFilter(e.target.value); setCurrentPage(1); }}
                            className="bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14] cursor-pointer"
                        >
                            <option value="">Всі ролі</option>
                            <option value="SuperAdmin">Глобальні Адміни</option>
                            <option value="CinemaManager">Менеджери філій</option>
                            <option value="Cashier">Касири</option>
                            <option value="Customer">Клієнти</option>
                        </select>

                        <select
                            value={cinemaFilter}
                            onChange={(e) => { setCinemaFilter(e.target.value); setCurrentPage(1); }}
                            className="bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14] cursor-pointer"
                        >
                            <option value="">Всі кінотеатри</option>
                            {cinemas.map(c => (
                                <option key={c.id} value={c.id}>{c.city} — {c.name}</option>
                            ))}
                        </select>
                    </div>

                    <button type="submit" className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black font-black text-xs px-6 py-3 rounded-xl uppercase tracking-wider transition-all flex items-center justify-center gap-2 border-none cursor-pointer">
                        <SlidersHorizontal className="w-4 h-4" /> Фільтрувати
                    </button>
                </form>
            </div>

            {isLoading ? (
                <div className="flex items-center justify-center py-24">
                    <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                </div>
            ) : users.length === 0 ? (
                <div className="text-center py-20 bg-[#1a1c26] rounded-2xl border border-white/5 p-8">
                    <p className="text-gray-400 text-sm">Користувачів за вказаними фільтрами не знайдено.</p>
                </div>
            ) : (
                <div className="bg-[#1a1c26] border border-white/5 rounded-2xl overflow-hidden shadow-xl">
                    <div className="overflow-x-auto">
                        <table className="w-full border-collapse text-left text-xs text-gray-300">
                            <thead className="bg-[#111219] font-bold text-gray-400 uppercase tracking-wider border-b border-white/5 text-[10px]">
                                <tr>
                                    <th className="p-4 sm:p-5">Користувач</th>
                                    <th className="p-4 sm:p-5">Email</th>
                                    <th className="p-4 sm:p-5">Прив'язка до локації</th>
                                    <th className="p-4 sm:p-5">Поточна Роль</th>
                                    <th className="p-4 sm:p-5 text-right">Дії</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-white/5">
                                {users.map((user) => {
                                    const currentRoleName = user.currentRole || 'Customer';
                                    return (
                                        <tr key={user.id} className="hover:bg-white/[0.02] transition-colors">
                                            <td className="p-4 sm:p-5 font-bold text-white whitespace-nowrap">
                                                {user.lastName} {user.firstName}
                                            </td>
                                            <td className="p-4 sm:p-5 text-gray-400 font-mono">{user.email}</td>
                                            <td className="p-4 sm:p-5 whitespace-nowrap">
                                                {user.cinemaId ? (
                                                    <span className="flex items-center gap-1.5 text-gray-300 font-medium">
                                                        <MapPin className="w-4 h-4 text-[#ffbd14]" /> {user.cinemaName || 'Кінотеатр'}
                                                    </span>
                                                ) : (
                                                    <span className="text-gray-500 italic">Глобальний доступ</span>
                                                )}
                                            </td>
                                            <td className="p-4 sm:p-5">
                                                <span className={`px-2.5 py-1 rounded-md text-[10px] font-black uppercase tracking-wider border ${
                                                    currentRoleName === 'SuperAdmin' ? 'bg-red-500/10 text-red-400 border-red-500/20' :
                                                    currentRoleName === 'CinemaManager' ? 'bg-[#ffbd14]/10 text-[#ffbd14] border-[#ffbd14]/20' :
                                                    currentRoleName === 'Cashier' ? 'bg-blue-500/10 text-blue-400 border-blue-500/20' :
                                                    'bg-white/5 text-gray-400 border-white/5'
                                                }`}>
                                                    {RoleLabels[currentRoleName] || currentRoleName}
                                                </span>
                                            </td>
                                            <td className="p-4 sm:p-5 text-right">
                                                <button
                                                    type="button"
                                                    onClick={() => handleOpenEditModal(user)}
                                                    className="p-2.5 bg-[#111219] hover:bg-[#ffbd14] rounded-xl text-gray-400 hover:text-black transition-all inline-flex items-center gap-2 border border-white/5 hover:border-transparent font-bold text-[11px] uppercase tracking-wider cursor-pointer"
                                                >
                                                    <Edit2 className="w-3.5 h-3.5" /> Налаштувати
                                                </button>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>

                    {totalPages > 1 && (
                        <div className="flex items-center justify-between p-4 sm:p-5 bg-[#111219]/40 border-t border-white/5 text-xs text-gray-400">
                            <div>Показано сторінку {currentPage} з {totalPages}</div>
                            <div className="flex items-center gap-2">
                                <button
                                    type="button"
                                    disabled={currentPage === 1}
                                    onClick={() => setCurrentPage(p => Math.max(p - 1, 1))}
                                    className="p-2 bg-[#111219] border border-white/5 rounded-lg disabled:opacity-30 hover:text-white transition-colors cursor-pointer"
                                >
                                    <ChevronLeft className="w-4 h-4" />
                                </button>
                                <button
                                    type="button"
                                    disabled={currentPage === totalPages}
                                    onClick={() => setCurrentPage(p => Math.min(p + 1, totalPages))}
                                    className="p-2 bg-[#111219] border border-white/5 rounded-lg disabled:opacity-30 hover:text-white transition-colors cursor-pointer"
                                >
                                    <ChevronRight className="w-4 h-4" />
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            )}

            <StaffRoleModal
                isOpen={isModalOpen}
                user={selectedUser}
                cinemas={cinemas}
                onClose={() => setIsModalOpen(false)}
                onSave={handleSaveRoleUpdate}
            />
        </div>
    );
};