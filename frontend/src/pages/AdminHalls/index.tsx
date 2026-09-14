import React, { useEffect, useState } from 'react';
import { useAuthStore } from '@/store/authStore';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { Plus, Trash2, Edit2, CheckCircle, XCircle, LayoutGrid, AlertCircle, Calendar, Armchair, Ban } from 'lucide-react';
import { hallsApi } from '@/api/halls';
import { cinemasApi } from '@/api/cinemas';
import { useToast } from '@/hooks/useToast';
import type { HallDto, CinemaDto, AdminSeatDto } from '@/types';
import { 
    SeatTypeMap, 
    SeatStatusMap, 
    SeatTypeReverseMap, 
    SeatStatusReverseMap 
} from '@/types';

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
    const [hallName, setHallName] = useState<string>('');
    const [totalRows, setTotalRows] = useState<number>(10);
    const [seatsPerRow, setSeatsPerRow] = useState<number>(12);
    const [isActive, setIsActive] = useState<boolean>(true);
    const [errorMessage, setErrorMessage] = useState<string>('');

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
        setHallName('');
        setTotalRows(8);
        setSeatsPerRow(10);
        setIsActive(true);
        setErrorMessage('');
        setIsModalOpen(true);
    };

    const handleEditOpen = (hall: HallDto) => {
        setEditingHall(hall);
        setHallName(hall.name);
        setTotalRows(hall.totalRows);
        setSeatsPerRow(hall.seatsPerRow);
        setIsActive(hall.isActive);
        setErrorMessage('');
        setIsModalOpen(true);
    };

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        setErrorMessage('');

        if (!hallName.trim()) {
            setErrorMessage('Назва залу не може бути порожньою');
            return;
        }

        const payload = {
            id: editingHall?.id || undefined,
            cinemaId: selectedCinemaId,
            name: hallName.trim(),
            totalRows: Number(totalRows),
            seatsPerRow: Number(seatsPerRow),
            isActive: isActive
        };

        try {
            if (editingHall) {
                await hallsApi.update(editingHall.id, payload);
                showSuccess('Геометрію залу успішно переконфігуровано.');
            } else {
                await hallsApi.create(payload);
                showSuccess('Новий кінозал успішно додано до системи.');
            }
            setIsModalOpen(false);
            fetchHalls();
        } catch (err: any) {
            setErrorMessage(err.response?.data?.message || 'Сталася помилка при збереженні залу.');
        }
    };

    const handleDelete = (id: string) => {
        confirm('Ви впевнені, що хочете повністю видалити цей зал разом із усіма місцями? Дію не можна буде скасувати.', async () => {
            try {
                await hallsApi.delete(id);
                showSuccess('Кінозал повністю видалено з системи.');
                fetchHalls();
            } catch (err: any) {
                showError(err.response?.data?.message || 'Не вдалося видалити зал. Можливо, до нього прив’язані активні сеанси.');
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

    const renderSeatRows = () => {
        const rowsMap: Record<string, AdminSeatDto[]> = {};
        hallSeats.forEach(seat => {
            if (!rowsMap[seat.row]) rowsMap[seat.row] = [];
            rowsMap[seat.row].push(seat);
        });

        return Object.entries(rowsMap)
            .sort(([a], [b]) => a.localeCompare(b, undefined, { numeric: true }))
            .map(([rowName, seats]) => (
                <div key={rowName} className="flex items-center gap-3 justify-center w-full">
                    <span className="w-5 text-[11px] font-black text-gray-500 text-center">{rowName}</span>
                    <div className="flex items-center gap-1.5">
                        {seats
                            .sort((a, b) => a.number - b.number)
                            .map(seat => {
                                const isBeingEdited = selectedSeatForEdit?.id === seat.id;

                                let seatClass = "bg-white/10 border-white/10 hover:border-[#ffbd14] text-white";
                                if (seat.status === 1) seatClass = "bg-red-500/10 border-red-500/40 text-red-400 hover:bg-red-500/20";
                                else if (seat.type === 1) seatClass = "bg-purple-600/20 border-purple-500/40 text-purple-400 hover:bg-purple-600/30";
                                else if (seat.type === 2) seatClass = "bg-blue-600/20 border-blue-500/40 text-blue-400 hover:bg-blue-600/30";

                                if (isBeingEdited) seatClass = "bg-[#ffbd14] border-[#ffbd14] text-black font-black ring-4 ring-[#ffbd14]/20 scale-105";

                                return (
                                    <button
                                        key={seat.id}
                                        type="button"
                                        onClick={() => setSelectedSeatForEdit(seat)}
                                        className={`w-7 h-7 rounded-md border text-[9px] font-black flex items-center justify-center transition-all cursor-pointer ${seatClass}`}
                                        title={`Ряд ${seat.row}, Місце ${seat.number} (${SeatTypeMap[seat.type]})`}
                                    >
                                        {seat.status === 1 ? <Ban className="w-2.5 h-2.5" /> : seat.number}
                                    </button>
                                );
                            })}
                    </div>
                    <span className="w-5 text-[11px] font-black text-gray-500 text-center">{rowName}</span>
                </div>
            ));
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
                                    <h3 className="text-lg font-black tracking-tight">🛠️ Конфігуратор технічних параметрів та типів крісел</h3>
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
                                    <div className="lg:col-span-3 flex flex-col items-center overflow-x-auto py-4 bg-[#111219]/40 border border-white/5 rounded-2xl p-6">
                                        <div className="w-full max-w-md bg-gradient-to-b from-[#ffbd14]/10 to-transparent h-3 rounded-t-full mb-12 relative flex items-center justify-center border-t border-[#ffbd14]/20">
                                            <span className="text-[9px] text-[#ffbd14]/40 font-bold tracking-[0.3em] uppercase absolute -bottom-5">Екран зали</span>
                                        </div>
                                        <div className="flex flex-col gap-2.5 min-w-[500px]">
                                            {renderSeatRows()}
                                        </div>
                                        <div className="flex flex-wrap justify-center gap-6 mt-10 border-t border-white/5 pt-5 w-full text-[11px] text-gray-400">
                                            <div className="flex items-center gap-2">
                                                <div className="w-3 h-3 bg-white/10 border border-white/10 rounded"></div>
                                                <span>Standard</span>
                                            </div>
                                            <div className="flex items-center gap-2">
                                                <div className="w-3 h-3 bg-purple-600/20 border border-purple-500/30 rounded"></div>
                                                <span>VIP (+50%)</span>
                                            </div>
                                            <div className="flex items-center gap-2">
                                                <div className="w-3 h-3 bg-blue-600/20 border border-blue-500/30 rounded"></div>
                                                <span>Disabled</span>
                                            </div>
                                            <div className="flex items-center gap-2">
                                                <div className="w-3 h-3 bg-red-500/10 border border-red-500/30 flex items-center justify-center rounded text-red-400"><Ban className="w-2 h-2" /></div>
                                                <span>Inactive (Зламане)</span>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="lg:col-span-1 bg-[#111219] border border-white/5 p-5 rounded-2xl flex flex-col gap-5 sticky top-6">
                                        {selectedSeatForEdit ? (
                                            <>
                                                <div>
                                                    <span className="text-[10px] text-[#ffbd14] font-black uppercase tracking-wider block">Обране місце</span>
                                                    <h4 className="text-xl font-black text-white mt-0.5">Ряд {selectedSeatForEdit.row}, Крісло {selectedSeatForEdit.number}</h4>
                                                </div>

                                                <div className="space-y-2">
                                                    <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider">Категорія (Тип)</label>
                                                    <div className="grid grid-cols-1 gap-2">
                                                        {(['Standard', 'VIP', 'Disabled'] as const).map(t => (
                                                            <button
                                                                key={t}
                                                                type="button"
                                                                onClick={() => handleUpdateSeatProperties(selectedSeatForEdit.id, t, SeatStatusMap[selectedSeatForEdit.status])}
                                                                className={`w-full py-2.5 px-4 text-left text-xs font-bold rounded-xl border transition-all cursor-pointer ${
                                                                    SeatTypeMap[selectedSeatForEdit.type] === t
                                                                        ? 'bg-purple-600 border-purple-500 text-white shadow-lg'
                                                                        : 'bg-[#1a1c26] border-white/5 text-gray-400 hover:text-white hover:border-white/10'
                                                                }`}
                                                            >
                                                                {t === 'Standard' && '🎟️ Standard (Звичайне)'}
                                                                {t === 'VIP' && '👑 VIP (Комфорт)'}
                                                                {t === 'Disabled' && '♿ Disabled (Інклюзивне)'}
                                                            </button>
                                                        ))}
                                                    </div>
                                                </div>

                                                <div className="space-y-2 border-t border-white/5 pt-4">
                                                    <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider">Технічний Стан</label>
                                                    <div className="grid grid-cols-2 gap-2">
                                                        {(['Active', 'Inactive'] as const).map(s => (
                                                            <button
                                                                key={s}
                                                                type="button"
                                                                onClick={() => handleUpdateSeatProperties(selectedSeatForEdit.id, SeatTypeMap[selectedSeatForEdit.type], s)}
                                                                className={`get-btn py-2 px-3 text-center text-xs font-black rounded-xl border transition-all uppercase tracking-wider cursor-pointer ${
                                                                    SeatStatusMap[selectedSeatForEdit.status] === s
                                                                        ? s === 'Active'
                                                                            ? 'bg-emerald-600 border-emerald-500 text-white'
                                                                            : 'bg-red-600 border-red-500 text-white'
                                                                        : 'bg-[#1a1c26] border-white/5 text-gray-400 hover:text-white'
                                                                }`}
                                                            >
                                                                {s === 'Active' ? '🟢 Active' : '🚫 Broken'}
                                                            </button>
                                                        ))}
                                                    </div>
                                                </div>
                                            </>
                                        ) : (
                                            <div className="text-center py-12 border border-dashed border-white/5 rounded-xl">
                                                <Armchair className="w-8 h-8 text-gray-600 mx-auto mb-2 opacity-50" />
                                                <p className="text-xs text-gray-400 italic px-4">Клацніть на будь-яке крісло ліворуч для налаштування</p>
                                            </div>
                                        )}
                                    </div>
                                </div>
                            )}
                        </div>
                    )}
                </div>
            )}

            {isModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
                    <div className="w-full max-w-md bg-[#1a1c26] border border-white/10 rounded-2xl p-6 relative">
                        <h2 className="text-xl font-black tracking-tight mb-4 text-white">
                            {editingHall ? '📝 Редагувати залу' : '✨ Створити новий зал'}
                        </h2>
                        {errorMessage && (
                            <p className="text-xs text-red-400 mb-3 bg-red-500/10 border border-red-500/10 p-2 rounded-lg font-bold">{errorMessage}</p>
                        )}
                        <form onSubmit={handleSave} className="space-y-4">
                            <div>
                                <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider mb-1">Назва залу</label>
                                <input
                                    type="text"
                                    value={hallName}
                                    onChange={(e) => setHallName(e.target.value)}
                                    placeholder="Наприклад: Синій зал, IMAX"
                                    className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14]"
                                />
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider mb-1">Всього рядів</label>
                                    <input
                                        type="number"
                                        value={totalRows}
                                        onChange={(e) => setTotalRows(Number(e.target.value))}
                                        disabled={!!editingHall}
                                        className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white disabled:opacity-50 disabled:cursor-not-allowed"
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider mb-1">Місць у ряді</label>
                                    <input
                                        type="number"
                                        value={seatsPerRow}
                                        onChange={(e) => setSeatsPerRow(Number(e.target.value))}
                                        disabled={!!editingHall}
                                        className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white disabled:opacity-50 disabled:cursor-not-allowed"
                                    />
                                </div>
                            </div>
                            <div className="flex items-center justify-end gap-3 pt-4 border-t border-white/5">
                                <button type="button" onClick={() => setIsModalOpen(false)} className="bg-white/5 text-white font-bold text-xs px-4 py-3 rounded-xl border-none cursor-pointer">Скасувати</button>
                                <button type="submit" className="bg-[#ffbd14] text-black font-black text-xs px-5 py-3 rounded-xl uppercase tracking-wider border-none cursor-pointer">Зберегти</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
};