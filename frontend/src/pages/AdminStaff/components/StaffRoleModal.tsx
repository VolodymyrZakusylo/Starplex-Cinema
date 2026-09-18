import React, { useState, useEffect } from 'react';
import { UserCheck, X, Check } from 'lucide-react';
import { UserRole, type UserRoleType, type UserStaffDto } from '@/types/admin';
import { getApiErrorMessage } from '@/api/errors';
import type { CinemaDto } from '@/types/cinemas';

const RoleNameToEnumMap: Record<string, UserRoleType> = {
    SuperAdmin: UserRole.SuperAdmin,
    CinemaManager: UserRole.CinemaManager,
    Cashier: UserRole.Cashier,
    Customer: UserRole.Customer
};

interface StaffRoleModalProps {
    isOpen: boolean;
    user: UserStaffDto | null;
    cinemas: CinemaDto[];
    onClose: () => void;
    onSave: (userId: string, newRole: UserRoleType, cinemaId: string | null) => Promise<void>;
}

export const StaffRoleModal: React.FC<StaffRoleModalProps> = ({
    isOpen,
    user,
    cinemas,
    onClose,
    onSave
}) => {
    const [selectedRole, setSelectedRole] = useState<UserRoleType>(UserRole.Customer);
    const [selectedCinemaId, setSelectedCinemaId] = useState<string>('');
    const [modalError, setModalError] = useState<string>('');
    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

    useEffect(() => {
        if (!isOpen || !user) return;

        const currentRoleName = user.currentRole || 'Customer';
        const roleEnum = RoleNameToEnumMap[currentRoleName] ?? UserRole.Customer;

        setSelectedRole(roleEnum);
        setSelectedCinemaId(user.cinemaId || '');
        setModalError('');
    }, [isOpen, user]);

    if (!isOpen || !user) return null;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setModalError('');

        const needCinema = selectedRole === UserRole.CinemaManager || selectedRole === UserRole.Cashier;
        if (needCinema && !selectedCinemaId) {
            setModalError('Необхідно обрати конкретну філію кінотеатру для цього співробітника.');
            return;
        }

        setIsSubmitting(true);
        try {
            await onSave(user.id, selectedRole, needCinema ? selectedCinemaId : null);
            onClose();
        } catch (err: any) {
            setModalError(getApiErrorMessage(err, 'Помилка при оновленні прав доступу.'));
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
            <div className="w-full max-w-md bg-[#1a1c26] border border-white/10 rounded-2xl shadow-2xl p-6 relative flex flex-col">
                <button
                    type="button"
                    onClick={onClose}
                    className="absolute top-4 right-4 text-gray-400 hover:text-white p-1 rounded-lg hover:bg-white/5 bg-transparent border-none cursor-pointer"
                >
                    <X className="w-5 h-5" />
                </button>

                <h2 className="text-xl font-black tracking-tight mb-1 text-white flex items-center gap-2">
                    <UserCheck className="text-[#ffbd14] w-5 h-5" /> Керування доступом
                </h2>
                <p className="text-xs text-gray-400 mb-4 font-mono truncate">{user.email}</p>

                {modalError && (
                    <div className="mb-4 bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-bold p-3 rounded-xl flex items-center gap-2">
                        <X className="w-4 h-4 flex-shrink-0 text-red-400" />
                        <div>{modalError}</div>
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-5 text-xs">
                    <div>
                        <label className="block text-gray-400 font-bold uppercase tracking-wider mb-2">Оберіть системну роль</label>
                        <select
                            value={selectedRole}
                            onChange={(e) => setSelectedRole(Number(e.target.value) as UserRoleType)}
                            className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-[#ffbd14] cursor-pointer"
                        >
                            <option value={UserRole.Customer}>Клієнт (Customer)</option>
                            <option value={UserRole.Cashier}>Касир (Cashier)</option>
                            <option value={UserRole.CinemaManager}>Менеджер філії (CinemaManager)</option>
                            <option value={UserRole.SuperAdmin}>Глобальний Адмін (SuperAdmin)</option>
                        </select>
                    </div>

                    {(selectedRole === UserRole.CinemaManager || selectedRole === UserRole.Cashier) && (
                        <div className="animate-fadeIn">
                            <label className="block text-gray-400 font-bold uppercase tracking-wider mb-2">Локація (Кінотеатр)</label>
                            <select
                                value={selectedCinemaId}
                                onChange={(e) => setSelectedCinemaId(e.target.value)}
                                className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-[#ffbd14] cursor-pointer"
                            >
                                <option value="">Оберіть зі списку філій...</option>
                                {cinemas.map(c => (
                                    <option key={c.id} value={c.id}>{c.city} — {c.name}</option>
                                ))}
                            </select>
                        </div>
                    )}

                    <div className="flex items-center justify-end gap-3 pt-4 border-t border-white/5">
                        <button
                            type="button"
                            onClick={onClose}
                            className="bg-white/5 hover:bg-white/10 text-white font-bold px-4 py-3 rounded-xl transition-all border-none cursor-pointer"
                        >
                            Скасувати
                        </button>
                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black font-black px-5 py-3 rounded-xl uppercase tracking-wider shadow-lg flex items-center gap-1.5 border-none cursor-pointer disabled:opacity-50"
                        >
                            <Check className="w-4 h-4 stroke-[3]" /> Зберегти зміни
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};
