import React, { useEffect, useState } from 'react';
import { getApiErrorMessage } from '@/api/errors';
import { useAuthStore } from '@/store/authStore';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { Plus, Trash2, Edit2, CheckCircle, XCircle, LayoutGrid, AlertCircle, Calendar, Armchair } from 'lucide-react';
import { hallsApi } from '@/api/halls';
import { cinemasApi } from '@/api/cinemas';
import { useToast } from '@/hooks/useToast';
import type { HallDto, CinemaDto, AdminSeatDto } from '@/types';
import { SeatTypeReverseMap, SeatStatusReverseMap } from '@/types';

import { HallFormModal, type HallFormPayload } from './components/HallFormModal';
import { HallSeatsGrid } from './components/HallSeatsGrid';
import { SeatEditPanel } from './components/SeatEditPanel';

export const AdminHallsPage: React.FC = () => {
    const { user } = useAuthStore();
    const navigate = useNavigate();
    const { confirm, showError, showSuccess } = useToast(); 
    
    const isSuperAdmin = user?.roles.includes('SuperAdmin');
    const [searchParams, setSearchParams] = useSearchParams();
    const cinemaIdFromUrl = searchParams.get('cinemaId');

    const [selectedCinemaId, setSelectedCinemaId] = useState<string>('');
    const [cinemas, setCinemas] = useState<CinemaDto[]>([]);
    const [halls, setHalls] = useState<HallDto[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [apiError, setApiError] = useState<string>('');

    const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
    const [editingHall, setEditingHall] = useState<HallDto | null>(null);

    const [activeHallIdForSeats, setActiveHallIdForSeats] = useState<string | null>(null);
    const [hallSeats, setHallSeats] = useState<AdminSeatDto[]>([]);
    const [isSeatsLoading, setIsSeatsLoading] = useState<boolean>(false);
    const [selectedSeatForEdit, setSelectedSeatForEdit] = useState<AdminSeatDto | null>(null);

    useEffect(() => {
        const initializePage = async () => {
            if (!user) return;
            setIsLoading(true);
            setApiError('');

            try {
                const userHasSuperAdmin = user.roles && user.roles.includes('SuperAdmin');

                if (userHasSuperAdmin) {
                    const data = await cinemasApi.getAll();
                    setCinemas(data);

                    if (data && data.length > 0) {
                        if (cinemaIdFromUrl && data.some(c => c.id === cinemaIdFromUrl)) {
                            setSelectedCinemaId(cinemaIdFromUrl);
                        } else {
                            setSelectedCinemaId(data[0].id);
                        }
                    } else {
                        setApiError('База даних повернула порожній список кінотеатрів.');
                        setIsLoading(false);
                    }
                } else {
                    if (user.cinemaId) {
                        setSelectedCinemaId(user.cinemaId);
                    } else {
                        setApiError('Помилка: у вашого аккаунта менеджера відсутня прив’язка до кінотеатру.');
                        setIsLoading(false);
                    }
                }
            } catch (err: any) {
                setApiError(err.response?.data?.message || `Не вдалося завантажити кінотеатри.`);
                setIsLoading(false);
            }
        };

        initializePage();
    }, [user, cinemaIdFromUrl]);

    const fetchHalls = async () => {
        if (!selectedCinemaId) return;
        setIsLoading(true);
        try {
            const data = await hallsApi.getByCinema(selectedCinemaId);
            setHalls(data);
            setActiveHallIdForSeats(null);
            setSelectedSeatForEdit(null);
        } catch (err: any) {
            showError(err.response?.data?.message || 'Не вдалося завантажити список залів.');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        if (selectedCinemaId) {
            fetchHalls();
        }
    }, [selectedCinemaId]);

    const handleManageSeatsOpen = async (hallId: string) => {
        if (activeHallIdForSeats === hallId) {
            setActiveHallIdForSeats(null);
            setSelectedSeatForEdit(null);
            return;
        }

        setActiveHallIdForSeats(hallId);
        setSelectedSeatForEdit(null);
        setIsSeatsLoading(true);
        try {
            const data = await hallsApi.getById(hallId);
            setHallSeats(data.seats || []);
        } catch (err) {
            showError('Не вдалося завантажити інтерактивну розкладку крісел зали.');
        } finally {
            setIsSeatsLoading(false);
        }
    };

    const handleUpdateSeatProperties = async (seatId: string, updatedTypeStr: 'Standard' | 'VIP' | 'Disabled', updatedStatusStr: 'Active' | 'Inactive') => {
        const numericType = SeatTypeReverseMap[updatedTypeStr];
        const numericStatus = SeatStatusReverseMap[updatedStatusStr];

        try {
            await hallsApi.updateSeatProperties(seatId, {
                type: numericType,
                status: numericStatus
            });

            setHallSeats(prev => prev.map(s => s.id === seatId ? { ...s, type: numericType, status: numericStatus } : s));

            if (selectedSeatForEdit && selectedSeatForEdit.id === seatId) {
                setSelectedSeatForEdit({ id: seatId, row: selectedSeatForEdit.row, number: selectedSeatForEdit.number, type: numericType, status: numericStatus });
            }
            showSuccess('Технічні параметри крісла успішно збережено.');
        } catch (err: any) {
            showError(err.response?.data?.message || 'Помилка при збереженні параметрів місця.');
        }
    };

    const handleCinemaChange = (cinemaId: string) => {
        setSelectedCinemaId(cinemaId);
        setSearchParams({ cinemaId });
    };

    const handleCreateOpen = () => {
        if (!selectedCinemaId) {
            showError('Будь ласка, спочатку оберіть або створіть кінотеатр.');
            return;
        }
        setEditingHall(null);
        setIsModalOpen(true);
    };

    const handleEditOpen = (hall: HallDto) => {
        setEditingHall(hall);
        setIsModalOpen(true);
    };

    const handleSaveHall = async (payload: HallFormPayload) => {
        if (editingHall) {
            await hallsApi.update(editingHall.id, payload);
            showSuccess('Геометрію залу успішно переконфігуровано.');
        } else {
            await hallsApi.create(payload);
            showSuccess('Новий кінозал успішно додано до системи.');
        }
        fetchHalls();
    };

    const handleDelete = (id: string) => {
        confirm('Ви впевнені, що хочете повністю видалити цей зал разом із усіма місцями? Дію не можна буде скасувати.', async () => {
            try {
                await hallsApi.delete(id);
                showSuccess('Кінозал повністю видалено з системи.');
                fetchHalls();
            } catch (err: any) {
                showError(getApiErrorMessage(err, 'Не вдалося видалити зал. Зал з історією сеансів можна деактивувати.'));
            }
        });
    };

    const toggleHallStatus = async (hall: HallDto) => {
        try {
            await hallsApi.update(hall.id, {
                id: hall.id,
                cinemaId: hall.cinemaId,
                name: hall.name,
                totalRows: hall.totalRows,
                seatsPerRow: hall.seatsPerRow,
                isActive: !hall.isActive
            });
            showSuccess(`Статус залу змінено на: ${!hall.isActive ? 'Активний' : 'Вимкнено'}`);
            fetchHalls();
        } catch (err) {
            showError('Не вдалося змінити операційний статус залу.');
        }
    };

    return (
        <div className="w-full select-none">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-6 mb-10 border-b border-white/5 pb-6">
                <div>
                    <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
                        <LayoutGrid className="text-[#ffbd14] w-8 h-8" /> Керування залами
                    </h1>
                    <p className="text-gray-400 text-sm mt-1">
                        Конфігурація геометрії залів кінотеатрів та операційне виведення крісел з експлуатації
                    </p>
                </div>

                <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-4 w-full sm:w-auto">
                    {isSuperAdmin && cinemas.length > 0 && (
                        <div className="flex flex-col">
                            <span className="text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-1 ml-1">Поточний кінотеатр</span>
                            <select
                                value={selectedCinemaId}
                                onChange={(e) => handleCinemaChange(e.target.value)}
                                className="bg-[#1a1c26] border border-white/10 rounded-xl px-4 py-2.5 text-sm font-semibold text-white focus:outline-none focus:border-[#ffbd14] transition-colors cursor-pointer"
                            >
                                {cinemas.map(c => (
                                    <option key={c.id} value={c.id}>{c.city} — {c.name}</option>
                                ))}
                            </select>
                        </div>
                    )}

                    <div className="flex items-end">
                        <button
                            onClick={handleCreateOpen}
                            className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black text-xs font-black px-5 py-3 rounded-xl transition-all shadow-lg flex items-center justify-center gap-2 uppercase tracking-wider h-[42px] mt-auto border-none cursor-pointer"
                        >
                            <Plus className="w-4 h-4 stroke-[3]" /> Додати зал
                        </button>
                    </div>
                </div>
            </div>

            {apiError && (
                <div className="mb-8 bg-red-500/10 border border-red-500/20 text-red-400 p-4 rounded-2xl flex items-center gap-3 text-sm">
                    <AlertCircle className="w-5 h-5 flex-shrink-0" />
                    <div><span className="font-bold">Помилка системи:</span> {apiError}</div>
                </div>
            )}

            {isLoading ? (
                <div className="flex items-center justify-center py-24">
                    <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                </div>
            ) : halls.length === 0 ? (
                <div className="text-center py-20 bg-[#1a1c26] rounded-2xl border border-white/5 p-8">
                    <p className="text-gray-400 text-sm">У цьому кінотеатрі ще не створено жодного залу.</p>
                </div>
            ) : (
                <div className="space-y-8">
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                        {halls.map((hall) => (
                            <div
                                key={hall.id}
                                className={`bg-[#1a1c26] border rounded-2xl p-6 shadow-xl relative overflow-hidden transition-all duration-300 ${
                                    hall.isActive ? 'border-white/5 hover:border-white/10' : 'border-red-500/20 bg-[#1a1c26]/50 opacity-75'
                                }`}
                            >
                                <div className="absolute top-6 right-6 flex items-center gap-2">
                                    <button
                                        type="button"
                                        onClick={() => toggleHallStatus(hall)}
                                        className="transition-transform hover:scale-105 cursor-pointer bg-transparent border-none"
                                    >
                                        {hall.isActive ? (
                                            <CheckCircle className="w-5 h-5 text-emerald-400" />
                                        ) : (
                                            <XCircle className="w-5 h-5 text-red-400" />
                                        )}
                                    </button>
                                </div>

                                <h3 className="text-xl font-black tracking-tight text-white mb-2">{hall.name}</h3>
                                <div className="space-y-1 text-sm text-gray-400 mb-6 font-medium">
                                    <p>Рядів у залі: <span className="text-white font-medium">{hall.totalRows}</span></p>
                                    <p>Місць у ряді: <span className="text-white font-medium">{hall.seatsPerRow}</span></p>
                                    <p>Загальна місткість: <span className="text-[#ffbd14] font-bold">{hall.totalCapacity} місць</span></p>
                                </div>

                                <div className="flex flex-wrap items-center justify-between gap-2 border-t border-white/5 pt-4">
                                    <button
                                        type="button"
                                        onClick={() => handleManageSeatsOpen(hall.id)}
                                        className={`text-xs font-bold px-3 py-1.5 rounded-lg border transition-all flex items-center gap-1.5 cursor-pointer ${
                                            activeHallIdForSeats === hall.id
                                                ? 'bg-[#ffbd14] border-[#ffbd14] text-black font-black'
                                                : 'bg-white/5 border-white/5 text-gray-300 hover:bg-white/10'
                                        }`}
                                    >
                                        <Armchair className="w-3.5 h-3.5" />
                                        <span>Місця залу</span>
                                    </button>

                                    <div className="flex items-center gap-1.5">
                                        <button
                                            type="button"
                                            onClick={() => navigate(`/admin/sessions?cinemaId=${hall.cinemaId}&hallId=${hall.id}`)}
                                            className="p-2 bg-white/5 hover:bg-[#ffbd14]/10 text-gray-400 hover:text-[#ffbd14] rounded-lg transition-colors border border-white/5 cursor-pointer"
                                            title="Переглянути розклад сеансів"
                                        >
                                            <Calendar className="w-4 h-4" />
                                        </button>
                                        <button
                                            type="button"
                                            onClick={() => handleEditOpen(hall)}
                                            className="p-2 bg-white/5 hover:bg-white/10 text-gray-400 hover:text-white rounded-lg transition-colors border border-white/5 cursor-pointer"
                                        >
                                            <Edit2 className="w-4 h-4" />
                                        </button>
                                        <button
                                            type="button"
                                            onClick={() => handleDelete(hall.id)}
                                            className="p-2 bg-red-500/10 hover:bg-red-500/20 text-red-400 rounded-lg transition-colors border border-red-500/20 cursor-pointer"
                                        >
                                            <Trash2 className="w-4 h-4" />
                                        </button>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>

                    {activeHallIdForSeats && (
                        <div className="bg-[#1a1c26] border border-white/5 rounded-2xl p-8 shadow-2xl animate-fadeIn">
                            <div className="w-full flex items-center justify-between border-b border-white/5 pb-4 mb-8">
                                <div>
                                    <h3 className="text-lg font-black tracking-tight">Конфігуратор технічних параметрів та типів крісел</h3>
                                    <p className="text-xs text-gray-400 mt-0.5">Оберіть будь-яке місце на схемі зали для зміни його категорії чи виведення з експлуатації</p>
                                </div>
                                <button
                                    type="button"
                                    onClick={() => { setActiveHallIdForSeats(null); setSelectedSeatForEdit(null); }}
                                    className="text-xs bg-white/5 border border-white/5 px-3 py-1.5 rounded-lg text-gray-400 hover:text-white hover:bg-white/10 cursor-pointer"
                                >
                                    Закрити пульт
                                </button>
                            </div>

                            {isSeatsLoading ? (
                                <div className="py-12 flex flex-col items-center gap-2">
                                    <div className="w-8 h-8 border-3 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                                    <span className="text-xs text-gray-400">Синхронізуємо геометрію залу...</span>
                                </div>
                            ) : (
                                <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
                                    <div className="lg:col-span-3">
                                        <HallSeatsGrid
                                            hallSeats={hallSeats}
                                            selectedSeatForEdit={selectedSeatForEdit}
                                            onSelectSeat={setSelectedSeatForEdit}
                                        />
                                    </div>

                                    <SeatEditPanel
                                        selectedSeat={selectedSeatForEdit}
                                        onUpdateSeatProperties={handleUpdateSeatProperties}
                                    />
                                </div>
                            )}
                        </div>
                    )}
                </div>
            )}

            <HallFormModal
                isOpen={isModalOpen}
                editingHall={editingHall}
                selectedCinemaId={selectedCinemaId}
                onClose={() => setIsModalOpen(false)}
                onSave={handleSaveHall}
            />
        </div>
    );
};
