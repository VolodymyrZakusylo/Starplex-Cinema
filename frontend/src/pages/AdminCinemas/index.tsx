import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Edit2, Trash2, MapPin, Film as CinemaIcon, AlertCircle, X, Check, Globe } from 'lucide-react';
import { cinemasApi } from '@/api/cinemas';
import type { CinemaDto } from '@/types/cinemas';
import { useToast } from '@/hooks/useToast';

export const AdminCinemasPage: React.FC = () => {
    const navigate = useNavigate();
    const { confirm, showError, showSuccess } = useToast();
    const [cinemas, setCinemas] = useState<CinemaDto[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
    const [isEditMode, setIsEditMode] = useState<boolean>(false);
    const [selectedCinemaId, setSelectedCinemaId] = useState<string>('');
    const [modalError, setModalError] = useState<string>('');

    const [name, setName] = useState<string>('');
    const [city, setCity] = useState<string>('');
    const [address, setAddress] = useState<string>('');

    const fetchCinemas = async () => {
        setIsLoading(true);
        try {
            const data = await cinemasApi.getAll();
            setCinemas(data);
        } catch (err: any) {
            showError(err.response?.data?.message || 'Не вдалося завантажити список кінотеатрів.');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchCinemas();
    }, []);

    const handleOpenCreateModal = () => {
        setIsEditMode(false);
        setSelectedCinemaId('');
        setName('');
        setCity('');
        setAddress('');
        setModalError('');
        setIsModalOpen(true);
    };

    const handleOpenEditModal = (cinema: CinemaDto, e: React.MouseEvent) => {
        e.stopPropagation();
        setIsEditMode(true);
        setSelectedCinemaId(cinema.id);
        setName(cinema.name);
        setCity(cinema.city);
        setAddress(cinema.address);
        setModalError('');
        setIsModalOpen(true);
    };

    const handleCancelDelete = (id: string, cinemaName: string, e: React.MouseEvent) => {
        e.stopPropagation();
        confirm(`Ви впевнені, що хочете видалити кінотеатр "${cinemaName}"? Це може вплинути на пов'язані зали та сеанси!`, async () => {
            try {
                await cinemasApi.delete(id);
                showSuccess('Кінотеатр успішно видалено з мережі.');
                fetchCinemas();
            } catch (err: any) {
                showError(err.response?.data?.message || 'Не вдалося видалити кінотеатр.');
            }
        });
    };

    const handleFormSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setModalError('');

        if (!name.trim() || !city.trim() || !address.trim()) {
            setModalError('Усі поля обовʼязкові для заповнення.');
            return;
        }

        try {
            if (isEditMode) {
                await cinemasApi.update(selectedCinemaId, {
                    id: selectedCinemaId,
                    name: name.trim(),
                    city: city.trim(),
                    address: address.trim()
                });
                showSuccess('Дані кінотеатру успішно оновлено.');
            } else {
                await cinemasApi.create({
                    name: name.trim(),
                    city: city.trim(),
                    address: address.trim()
                });
                showSuccess('Нову філію кінотеатру успішно додано.');
            }
            setIsModalOpen(false);
            fetchCinemas();
        } catch (err: any) {
            setModalError(err.response?.data?.message || 'Помилка при збереженні даних кінотеатру.');
        }
    };

    return (
        <div className="w-full select-none">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-6 mb-10 border-b border-white/5 pb-6">
                <div>
                    <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
                        <Globe className="text-[#ffbd14] w-8 h-8" /> Мережа Кінотеатрів
                    </h1>
                    <p className="text-gray-400 text-sm mt-1">Керування філіями кінотеатрів StarPlex, містами та локаціями</p>
                </div>
                <button 
                    onClick={handleOpenCreateModal}
                    className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black font-black text-xs px-5 py-3.5 rounded-xl uppercase tracking-wider transition-all flex items-center gap-2 shadow-lg border-none cursor-pointer"
                >
                    <Plus className="w-4 h-4 stroke-[3]" /> Додати кінотеатр
                </button>
            </div>

            {isLoading ? (
                <div className="flex items-center justify-center py-24">
                    <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                </div>
            ) : cinemas.length === 0 ? (
                <div className="text-center py-20 bg-[#1a1c26] rounded-2xl border border-white/5 p-8">
                    <p className="text-gray-400 text-sm">Жодного кінотеатру ще не створено у базі даних.</p>
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {cinemas.map((cinema) => (
                        <div 
                            key={cinema.id} 
                            onClick={() => navigate(`/admin/halls?cinemaId=${cinema.id}`)}
                            className="bg-[#1a1c26] border border-white/5 hover:border-[#ffbd14]/30 rounded-2xl p-6 flex flex-col justify-between shadow-xl relative overflow-hidden group cursor-pointer transition-all duration-300 hover:-translate-y-1"
                        >
                            <div className="absolute top-0 left-0 w-1 h-full bg-[#ffbd14] opacity-0 group-hover:opacity-100 transition-opacity"></div>
                            
                            <div>
                                <div className="flex items-start justify-between gap-4 mb-4">
                                    <div className="p-3 bg-[#111219] rounded-xl border border-white/5 text-[#ffbd14]">
                                        <CinemaIcon className="w-6 h-6" />
                                    </div>
                                    <div className="flex items-center gap-1.5">
                                        <button 
                                            onClick={(e) => handleOpenEditModal(cinema, e)}
                                            className="p-2 bg-[#111219] hover:bg-white/5 rounded-lg text-gray-400 hover:text-white transition-colors border border-white/5 cursor-pointer"
                                        >
                                            <Edit2 className="w-3.5 h-3.5" />
                                        </button>
                                        <button 
                                            onClick={(e) => handleCancelDelete(cinema.id, cinema.name, e)}
                                            className="p-2 bg-[#111219] hover:bg-red-500/10 rounded-lg text-gray-500 hover:text-red-400 transition-colors border border-white/5 cursor-pointer"
                                        >
                                            <Trash2 className="w-3.5 h-3.5" />
                                        </button>
                                    </div>
                                </div>

                                <h3 className="text-xl font-black text-white mb-2 tracking-tight truncate">{cinema.name}</h3>
                                
                                <div className="space-y-2 text-sm text-gray-400 font-medium">
                                    <p className="flex items-center gap-2">
                                        <MapPin className="w-4 h-4 text-[#ffbd14] flex-shrink-0" />
                                        <span className="text-white">{cinema.city}</span>
                                    </p>
                                    <p className="text-xs text-gray-500 pl-6 leading-relaxed line-clamp-2">{cinema.address}</p>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {isModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
                    <div className="w-full max-w-md bg-[#1a1c26] border border-white/10 rounded-2xl shadow-2xl p-6 relative flex flex-col">
                        <button 
                            onClick={() => setIsModalOpen(false)}
                            className="absolute top-4 right-4 text-gray-400 hover:text-white p-1 rounded-lg hover:bg-white/5 bg-transparent border-none cursor-pointer"
                        >
                            <X className="w-5 h-5" />
                        </button>

                        <h2 className="text-xl font-black tracking-tight mb-4 text-white flex items-center gap-2">
                            <CinemaIcon className="text-[#ffbd14] w-5 h-5" /> 
                            {isEditMode ? 'Редагувати філію' : 'Новий кінотеатр'}
                        </h2>

                        {modalError && (
                            <div className="mb-4 bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-bold p-3 rounded-xl flex items-center gap-2">
                                <AlertCircle className="w-4 h-4 flex-shrink-0" />
                                <div>{modalError}</div>
                            </div>
                        )}

                        <form onSubmit={handleFormSubmit} className="space-y-4">
                            <div>
                                <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider mb-2">Назва кінотеатру</label>
                                <input
                                    type="text"
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                    placeholder="Напр., StarPlex Центр"
                                    className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14]"
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider mb-2">Місто</label>
                                <input
                                    type="text"
                                    value={city}
                                    onChange={(e) => setCity(e.target.value)}
                                    placeholder="Напр., Житомир"
                                    className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14]"
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider mb-2">Повна адреса</label>
                                <textarea
                                    value={address}
                                    onChange={(e) => setAddress(e.target.value)}
                                    placeholder="Напр., вул. Михайлівська, 12"
                                    rows={3}
                                    className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14] resize-none"
                                    required
                                />
                            </div>

                            <div className="flex items-center justify-end gap-3 pt-4 border-t border-white/5">
                                <button 
                                    type="button" 
                                    onClick={() => setIsModalOpen(false)} 
                                    className="bg-white/5 hover:bg-white/10 text-white font-bold text-xs px-4 py-3 rounded-xl transition-all border-none cursor-pointer"
                                >
                                    Скасувати
                                </button>
                                <button 
                                    type="submit" 
                                    className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black font-black text-xs px-5 py-3 rounded-xl uppercase tracking-wider shadow-lg flex items-center gap-1.5 border-none cursor-pointer"
                                >
                                    <Check className="w-4 h-4 stroke-[3]" /> Зберегти
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
};