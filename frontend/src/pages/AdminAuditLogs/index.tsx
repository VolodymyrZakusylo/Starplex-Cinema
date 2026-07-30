import React, { useEffect, useState, useCallback } from 'react';
import { ShieldAlert, RefreshCw, Layers, Calendar, UserCheck, Activity, Search, FilterX, ChevronLeft, ChevronRight } from 'lucide-react';
import api from '@/api/axios';
import { useToast } from '@/hooks/useToast';

interface AuditLogDto {
    id: string;
    userId: string;
    userEmail: string;
    action: string;
    entityName: string;
    entityId: string;
    timestamp: string;
}

interface PagedResponse {
    items: AuditLogDto[];
    pageNumber: number;
    totalPages: number;
    totalCount: number;
}

export const AdminAuditLogsPage: React.FC = () => {
    const { showError } = useToast();
    const [logsData, setLogsData] = useState<PagedResponse | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const [searchUser, setSearchUser] = useState<string>('');
    const [selectedEntity, setSelectedEntity] = useState<string>('ALL');
    const [selectedAction, setSelectedAction] = useState<string>('ALL');
    const [currentPage, setCurrentPage] = useState<number>(1);

    const loadData = useCallback(async (page: number, entity: string, action: string, user: string) => {
        setIsLoading(true);
        try {
            const response = await api.get<PagedResponse>('/Audit/logs', {
                params: {
                    pageNumber: page,
                    pageSize: 15,
                    searchUser: user || undefined,
                    entityName: entity,
                    actionType: action
                }
            });
            setLogsData(response.data);
        } catch (err: any) {
            showError(err.response?.data?.message || 'Не вдалося завантажити системні логи.');
        } finally {
            setIsLoading(false);
        }
    }, [showError]);

    useEffect(() => {
        loadData(1, 'ALL', 'ALL', '');
    }, [loadData]);

    const handlePageChange = (newPage: number) => {
        setCurrentPage(newPage);
        loadData(newPage, selectedEntity, selectedAction, searchUser);
    };

    const handleFilterChange = (entity: string, action: string) => {
        setSelectedEntity(entity);
        setSelectedAction(action);
        setCurrentPage(1);
        loadData(1, entity, action, searchUser);
    };

    const handleTextSearchSubmit = () => {
        setCurrentPage(1);
        loadData(1, selectedEntity, selectedAction, searchUser);
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') {
            handleTextSearchSubmit();
        }
    };

    const resetFilters = () => {
        setSearchUser('');
        setSelectedEntity('ALL');
        setSelectedAction('ALL');
        setCurrentPage(1);
        loadData(1, 'ALL', 'ALL', '');
    };

    const getActionBadge = (action: string) => {
        if (action.includes("TicketRefund") || action.includes("Delete")) {
            return "bg-red-500/10 border-red-500/20 text-red-400";
        }
        if (action.includes("Added") || action.includes("Create")) {
            return "bg-emerald-500/10 border-emerald-500/20 text-emerald-400";
        }
        return "bg-amber-500/10 border-amber-500/20 text-amber-400";
    };

    return (
        <div className="w-full select-none">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-6 mb-8 border-b border-white/5 pb-6">
                <div>
                    <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
                        <ShieldAlert className="text-[#ffbd14] w-8 h-8" /> Аудит безпеки системи
                    </h1>
                    <p className="text-gray-400 text-sm mt-1">
                        Журнал фіксації дій персоналу (Пагінація та фільтрація на стороні сервера PostgreSQL)
                    </p>
                </div>

                <button
                    onClick={() => loadData(currentPage, selectedEntity, selectedAction, searchUser)}
                    className="bg-dark-secondary border border-white/10 hover:border-[#ffbd14] text-white text-xs font-bold px-4 py-3 rounded-xl transition-all flex items-center gap-2 cursor-pointer border-none shadow-md active:scale-95 shrink-0"
                >
                    <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin text-[#ffbd14]' : ''}`} /> 
                    Оновити журнал
                </button>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6 bg-dark-secondary border border-white/5 p-4 rounded-2xl shadow-md">
                <div className="relative flex items-center">
                    <Search className="w-4 h-4 text-gray-500 absolute left-3.5 pointer-events-none" />
                    <input
                        type="text"
                        placeholder="Введіть ID та натисніть Enter..."
                        value={searchUser}
                        onChange={(e) => setSearchUser(e.target.value)}
                        onKeyDown={handleKeyDown}
                        className="w-full bg-[#161720] border border-white/10 rounded-xl pl-10 pr-4 py-2.5 text-xs text-gray-200 placeholder-gray-500 focus:outline-none focus:border-[#ffbd14] transition-colors"
                    />
                </div>

                <div className="flex items-center">
                    <select
                        value={selectedEntity}
                        onChange={(e) => handleFilterChange(e.target.value, selectedAction)}
                        className="w-full bg-[#161720] border border-white/10 rounded-xl px-3 py-2.5 text-xs text-gray-200 focus:outline-none focus:border-[#ffbd14] cursor-pointer transition-colors"
                    >
                        <option value="ALL">Всі об'єкти (Таблиці)</option>
                        <option value="Cinema">Cinema (Кінотеатри)</option>
                        <option value="Hall">Hall (Зали)</option>
                        <option value="Session">Session (Сеанси/Розклад)</option>
                        <option value="Booking">Booking (Замовлення квитків)</option>
                    </select>
                </div>

                <div className="flex items-center">
                    <select
                        value={selectedAction}
                        onChange={(e) => handleFilterChange(selectedEntity, e.target.value)}
                        className="w-full bg-[#161720] border border-white/10 rounded-xl px-3 py-2.5 text-xs text-gray-200 focus:outline-none focus:border-[#ffbd14] cursor-pointer transition-colors"
                    >
                        <option value="ALL">Всі типи операцій</option>
                        <option value="Added">Added (Створення)</option>
                        <option value="Modified">Modified (Редагування)</option>
                        <option value="REFUND">TicketRefund (Повернення коштів)</option>
                    </select>
                </div>

                <div className="flex items-center gap-2">
                    <button
                        onClick={handleTextSearchSubmit}
                        className="w-1/2 py-2.5 bg-[#ffbd14] hover:bg-[#e0a410] text-black font-black rounded-xl text-xs transition-all cursor-pointer shadow-lg active:scale-95 border-none uppercase tracking-wider"
                    >
                        Пошук
                    </button>
                    <button
                        onClick={resetFilters}
                        className="w-1/2 py-2.5 bg-white/5 hover:bg-white/10 border border-white/10 text-gray-300 rounded-xl text-xs font-bold transition-all flex items-center justify-center gap-1.5 cursor-pointer active:scale-95"
                    >
                        <FilterX className="w-3.5 h-3.5" /> Скинути
                    </button>
                </div>
            </div>

            {isLoading ? (
                <div className="flex items-center justify-center py-40">
                    <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                </div>
            ) : !logsData || logsData.items.length === 0 ? (
                <div className="text-center py-24 bg-dark-secondary border border-white/5 rounded-3xl p-8">
                    <p className="text-gray-500 italic text-sm">Збігів у базі даних PostgreSQL не знайдено.</p>
                </div>
            ) : (
                <>
                    <div className="w-full overflow-x-auto rounded-2xl border border-white/5 bg-dark-secondary shadow-2xl">
                        <table className="w-full text-left border-collapse text-xs">
                            <thead>
                                <tr className="border-b border-white/5 bg-white/[0.02] text-gray-400 font-bold uppercase tracking-wider text-[10px]">
                                    <th className="p-4 flex items-center gap-1.5"><Calendar className="w-3.5 h-3.5" /> Час події (Local)</th>
                                    <th className="p-4"><UserCheck className="w-3.5 h-3.5 inline mr-1.5" /> Ініціатор події</th>
                                    <th className="p-4"><Layers className="w-3.5 h-3.5 inline mr-1.5" /> Об'єкт (Entity)</th>
                                    <th className="p-4 text-center"><Activity className="w-3.5 h-3.5 inline mr-1.5" /> Тип операції</th>
                                    <th className="p-4 font-mono">Ідентифікатор Target ID</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-white/[0.02]">
                                {logsData.items.map((log) => (
                                    <tr key={log.id} className="hover:bg-white/[0.01] transition-colors">
                                        <td className="p-4 text-gray-400 font-medium">
                                            {new Date(log.timestamp).toLocaleString('uk-UA')}
                                        </td>
                                        <td className="p-4 font-bold text-gray-200">{log.userEmail}</td>
                                        <td className="p-4">
                                            <span className="bg-white/5 px-2 py-1 rounded text-gray-300 border border-white/5 font-semibold">
                                                {log.entityName}
                                            </span>
                                        </td>
                                        <td className="p-4 text-center">
                                            <span className={`px-2.5 py-0.5 rounded-full border text-[10px] font-black uppercase tracking-wider ${getActionBadge(log.action)}`}>
                                                {log.action}
                                            </span>
                                        </td>
                                        <td className="p-4 font-mono text-[11px] text-gray-500 tracking-tight">{log.entityId}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>

                    {logsData.totalPages > 1 && (
                        <div className="flex items-center justify-between mt-5 bg-dark-secondary border border-white/5 px-4 py-3 rounded-xl shadow-md text-xs">
                            <div className="text-gray-400">
                                Сторінка <span className="text-[#ffbd14] font-black">{logsData.pageNumber}</span> з{" "}
                                <span className="text-white font-bold">{logsData.totalPages}</span> (Всього записів у базі:{" "}
                                <span className="text-white font-bold">{logsData.totalCount}</span>)
                            </div>
                            <div className="flex items-center gap-2">
                                <button
                                    onClick={() => handlePageChange(Math.max(currentPage - 1, 1))}
                                    disabled={currentPage === 1}
                                    className="p-2 bg-white/5 hover:bg-white/10 disabled:opacity-30 disabled:hover:bg-white/5 border border-white/10 rounded-lg text-white transition-all cursor-pointer"
                                >
                                    <ChevronLeft className="w-4 h-4" />
                                </button>
                                <button
                                    onClick={() => handlePageChange(Math.min(currentPage + 1, logsData.totalPages))}
                                    disabled={currentPage === logsData.totalPages}
                                    className="p-2 bg-white/5 hover:bg-white/10 disabled:opacity-30 disabled:hover:bg-white/5 border border-white/10 rounded-lg text-white transition-all cursor-pointer"
                                >
                                    <ChevronRight className="w-4 h-4" />
                                </button>
                            </div>
                        </div>
                    )}
                </>
            )}
        </div>
    );
};