import React, { useEffect, useState } from 'react';
import { BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend, PieChart, Pie, Cell } from 'recharts';
import { DollarSign, Ticket, Calendar, RefreshCcw, TrendingUp, Building2, Monitor } from 'lucide-react';
import api from '@/api/axios';
import { useToast } from '@/hooks/useToast';

interface RevenueChartItem {
    date: string;
    revenue: number;
    ticketsCount: number;
}

interface MoviePopularity {
    movieTitle: string;
    ticketsSold: number;
    earnings: number;
}

interface SeatTypeBreakdown {
    type: string;
    count: number;
    revenue: number;
}

interface AdminStatsDto {
    totalRevenue: number;
    totalTicketsSold: number;
    activeSessionsCount: number;
    refundedAmount: number;
    averageOrderValue: number;
    revenueChart: RevenueChartItem[];
    topMovies: MoviePopularity[];
    seatTypeStats: SeatTypeBreakdown[];
}

interface CinemaDto {
    id: string;
    name: string;
}

const COLORS = ['#3B82F6', '#10B981', '#EF4444', '#F59E0B'];

export const AdminDashboardPage: React.FC = () => {
    const { showError } = useToast();
    const [stats, setStats] = useState<AdminStatsDto | null>(null);
    const [daysPeriod, setDaysPeriod] = useState<number>(14);
    const [cinemas, setCinemas] = useState<CinemaDto[]>([]);
    const [selectedCinema, setSelectedCinema] = useState<string>('all');
    const [salesSource, setSalesSource] = useState<string>('all');
    const [isLoading, setIsLoading] = useState<boolean>(true);

    useEffect(() => {
        const fetchCinemas = async () => {
            try {
                const response = await api.get<CinemaDto[]>('/Cinemas');
                setCinemas(response.data);
            } catch (err) {
                showError('Не вдалося завантажити перелік кінотеатрів мережі.');
            }
        };
        fetchCinemas();
    }, []);

    useEffect(() => {
        const fetchStats = async () => {
            setIsLoading(true);
            try {
                let url = `/Analytics/admin-stats?days=${daysPeriod}&isOnline=${salesSource}`;
                if (selectedCinema !== 'all') {
                    url += `&cinemaId=${selectedCinema}`;
                }
                const response = await api.get<AdminStatsDto>(url);
                setStats(response.data);
            } catch (err) {
                showError('Помилка при оновленні фінансових аналітичних даних.');
            } finally {
                setIsLoading(false);
            }
        };

        fetchStats();
    }, [daysPeriod, selectedCinema, salesSource]);

    const formatDate = (dateStr: string) => {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        return date.toLocaleDateString('uk-UA', { day: 'numeric', month: 'short' });
    };

    if (isLoading && !stats) {
        return (
            <div className="min-h-[60vh] flex items-center justify-center text-white">
                <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
            </div>
        );
    }

    return (
        <div className="w-full select-none flex flex-col gap-8">

            <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-6 border-b border-white/5 pb-6">
                <div>
                    <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
                        <TrendingUp className="text-[#ffbd14] w-8 h-8" /> Фінансовий Моніторинг
                    </h1>
                    <p className="text-gray-400 text-sm mt-1">Аналітичні дані мережі кінотеатрів StarPlex в реальному часі</p>
                </div>

                <div className="flex bg-[#161922] p-1 rounded-xl border border-white/5 shadow-inner">
                    {[7, 14, 30].map((days) => (
                        <button
                            key={days}
                            onClick={() => setDaysPeriod(days)}
                            className={`px-4 py-2 text-xs font-bold rounded-lg transition-all border-none cursor-pointer ${daysPeriod === days
                                    ? 'bg-[#ffbd14] text-black shadow-md'
                                    : 'text-gray-400 hover:text-white bg-transparent'
                                }`}
                        >
                            {days} днів
                        </button>
                    ))}
                </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 bg-[#141622] p-4 rounded-2xl border border-white/5 shadow-md">
                <div className="flex flex-col gap-1.5">
                    <label className="text-[11px] uppercase tracking-wider font-bold text-gray-400 flex items-center gap-1.5">
                        <Building2 className="w-3.5 h-3.5 text-[#ffbd14]" /> Філіал кінотеатру
                    </label>
                    <select
                        value={selectedCinema}
                        onChange={(e) => setSelectedCinema(e.target.value)}
                        className="bg-[#1a1c26] border border-white/10 rounded-xl px-3 py-2.5 text-xs font-bold text-white focus:outline-none focus:border-[#ffbd14] transition-colors cursor-pointer"
                    >
                        <option value="all">Всі кінотеатри мережі</option>
                        {cinemas.map((c) => (
                            <option key={c.id} value={c.id}>{c.name}</option>
                        ))}
                    </select>
                </div>

                <div className="flex flex-col gap-1.5">
                    <label className="text-[11px] uppercase tracking-wider font-bold text-gray-400 flex items-center gap-1.5">
                        <Monitor className="w-3.5 h-3.5 text-[#ffbd14]" /> Канал дистриб'юції
                    </label>
                    <select
                        value={salesSource}
                        onChange={(e) => setSalesSource(e.target.value)}
                        className="bg-[#1a1c26] border border-white/10 rounded-xl px-3 py-2.5 text-xs font-bold text-white focus:outline-none focus:border-[#ffbd14] transition-colors cursor-pointer"
                    >
                        <option value="all">Всі продажі (Онлайн + Каса)</option>
                        <option value="online">Тільки Онлайн (Сайт / Додаток)</option>
                        <option value="boxoffice">Тільки Фізична каса кінотеатру</option>
                    </select>
                </div>
            </div>

            {stats && (
                <>
                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
                        <div className="bg-[#141622] border border-white/5 p-6 rounded-2xl shadow-xl flex items-center justify-between">
                            <div>
                                <span className="text-xs text-gray-400 uppercase tracking-wider font-semibold">Чиста виручка</span>
                                <h3 className="text-2xl font-black text-white mt-1">{stats.totalRevenue.toFixed(2)} ₴</h3>
                                <p className="text-[10px] text-emerald-400 font-bold mt-1">Сер. чек: {stats.averageOrderValue} ₴</p>
                            </div>
                            <div className="w-12 h-12 bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 rounded-xl flex items-center justify-center shadow-inner">
                                <DollarSign className="w-6 h-6" />
                            </div>
                        </div>

                        <div className="bg-[#141622] border border-white/5 p-6 rounded-2xl shadow-xl flex items-center justify-between">
                            <div>
                                <span className="text-xs text-gray-400 uppercase tracking-wider font-semibold">Продано квитків</span>
                                <h3 className="text-2xl font-black text-white mt-1">{stats.totalTicketsSold} шт.</h3>
                                <p className="text-[10px] text-blue-400 font-bold mt-1">Заповнюваність залів стабільна</p>
                            </div>
                            <div className="w-12 h-12 bg-[#ffbd14]/10 border border-[#ffbd14]/20 text-[#ffbd14] rounded-xl flex items-center justify-center shadow-inner">
                                <Ticket className="w-6 h-6" />
                            </div>
                        </div>

                        <div className="bg-[#141622] border border-white/5 p-6 rounded-2xl shadow-xl flex items-center justify-between">
                            <div>
                                <span className="text-xs text-gray-400 uppercase tracking-wider font-semibold">Активні сеанси</span>
                                <h3 className="text-2xl font-black text-white mt-1">{stats.activeSessionsCount}</h3>
                                <p className="text-[10px] text-gray-400 mt-1">Готові до показу</p>
                            </div>
                            <div className="w-12 h-12 bg-blue-500/10 border border-blue-500/20 text-blue-400 rounded-xl flex items-center justify-center shadow-inner">
                                <Calendar className="w-6 h-6" />
                            </div>
                        </div>

                        <div className="bg-[#141622] border border-white/5 p-6 rounded-2xl shadow-xl flex items-center justify-between">
                            <div>
                                <span className="text-xs text-gray-400 uppercase tracking-wider font-semibold">Повернення коштів</span>
                                <h3 className="text-2xl font-black text-red-400 mt-1">{stats.refundedAmount.toFixed(2)} ₴</h3>
                                <p className="text-[10px] text-red-400/70 mt-1">Скасовані квитки</p>
                            </div>
                            <div className="w-12 h-12 bg-red-500/10 border border-red-500/20 text-red-400 rounded-xl flex items-center justify-center shadow-inner">
                                <RefreshCcw className="w-6 h-6" />
                            </div>
                        </div>
                    </div>

                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                        <div className="lg:col-span-2 bg-[#141622] border border-white/5 p-6 rounded-2xl shadow-xl">
                            <h2 className="text-sm font-black uppercase tracking-wider text-gray-300 mb-6 flex items-center gap-2">
                                <TrendingUp className="w-4 h-4 text-[#ffbd14]" /> Динаміка продажів та виручки
                            </h2>
                            <div className="w-full h-80">
                                {stats.revenueChart.length === 0 ? (
                                    <div className="w-full h-full flex items-center justify-center text-gray-500 text-sm">Немає фінансових даних за обраний період</div>
                                ) : (
                                    <ResponsiveContainer width="100%" height="100%">
                                        <LineChart data={stats.revenueChart} margin={{ top: 10, right: 10, left: -10, bottom: 0 }}>
                                            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.03)" />
                                            <XAxis dataKey="date" tickFormatter={formatDate} stroke="#6B7280" style={{ fontSize: '10px', fontWeight: 'bold' }} />
                                            <YAxis yAxisId="left" stroke="#6B7280" style={{ fontSize: '10px' }} />
                                            <YAxis yAxisId="right" orientation="right" stroke="#6B7280" style={{ fontSize: '10px' }} />
                                            <Tooltip
                                                contentStyle={{ backgroundColor: '#141622', borderColor: 'rgba(255,255,255,0.05)', borderRadius: '12px' }}
                                                labelStyle={{ color: '#9CA3AF', fontWeight: 'bold' }}
                                            />
                                            <Legend wrapperStyle={{ fontSize: '11px', paddingTop: '10px' }} />
                                            <Line yAxisId="left" type="monotone" dataKey="revenue" name="Виручка (₴)" stroke="#ffbd14" strokeWidth={3} activeDot={{ r: 6 }} />
                                            <Line yAxisId="right" type="monotone" dataKey="ticketsCount" name="Квитки (шт)" stroke="#3B82F6" strokeWidth={2} />
                                        </LineChart>
                                    </ResponsiveContainer>
                                )}
                            </div>
                        </div>

                        <div className="bg-[#141622] border border-white/5 p-6 rounded-2xl shadow-xl flex flex-col justify-between">
                            <h2 className="text-sm font-black uppercase tracking-wider text-gray-300 mb-4">💺 Структура проданих місць</h2>
                            <div className="w-full h-56 relative flex items-center justify-center">
                                <ResponsiveContainer width="100%" height="100%">
                                    <PieChart>
                                        <Pie
                                            data={stats.seatTypeStats}
                                            cx="50%"
                                            cy="50%"
                                            innerRadius={60}
                                            outerRadius={80}
                                            paddingAngle={4}
                                            dataKey="count"
                                            nameKey="type"
                                        >
                                            {stats.seatTypeStats.map((entry, index) => (
                                                <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                                            ))}
                                        </Pie>
                                        <Tooltip contentStyle={{ backgroundColor: '#141622', borderRadius: '8px', border: 'none' }} />
                                    </PieChart>
                                </ResponsiveContainer>
                            </div>
                            <div className="flex flex-col gap-2 mt-4 border-t border-white/5 pt-4">
                                {stats.seatTypeStats.map((stat, idx) => (
                                    <div key={stat.type} className="flex justify-between items-center text-xs">
                                        <div className="flex items-center gap-2 font-bold text-gray-300">
                                            <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: COLORS[idx % COLORS.length] }}></span>
                                            {stat.type === 'VIP' ? 'VIP місця' : stat.type === 'Disabled' ? 'Інклюзивні' : 'Стандарт'}
                                        </div>
                                        <span className="font-mono text-gray-400">{stat.count} квитків ({stat.revenue} ₴)</span>
                                    </div>
                                ))}
                            </div>
                        </div>

                        <div className="lg:col-span-3 bg-[#141622] border border-white/5 p-6 rounded-2xl shadow-xl">
                            <h2 className="text-sm font-black uppercase tracking-wider text-gray-300 mb-6">
                                🎬 Рейтинг прокату фільмів за кількістю місць
                            </h2>
                            <div className="w-full h-72">
                                {stats.topMovies.length === 0 ? (
                                    <div className="w-full h-full flex items-center justify-center text-gray-500 text-xs">Немає прокатної статистики</div>
                                ) : (
                                    <ResponsiveContainer width="100%" height="100%">
                                        <BarChart
                                            data={stats.topMovies}
                                            layout="vertical"
                                            margin={{ top: 10, right: 30, left: 60, bottom: 10 }}
                                        >
                                            <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.03)" horizontal={false} />
                                            <XAxis type="number" stroke="#6B7280" style={{ fontSize: '10px', fontWeight: 'bold' }} />
                                            <YAxis
                                                dataKey="movieTitle"
                                                type="category"
                                                stroke="#FFF"
                                                style={{ fontSize: '11px', fontWeight: '900' }}
                                                width={120}
                                            />
                                            <Tooltip
                                                contentStyle={{ backgroundColor: '#141622', borderRadius: '12px', borderColor: 'rgba(255,255,255,0.05)' }}
                                                labelStyle={{ color: '#9CA3AF', fontWeight: 'bold' }}
                                            />
                                            <Bar dataKey="ticketsSold" name="Продано квитків" fill="#ffbd14" radius={[0, 6, 6, 0]} maxBarSize={24} />
                                        </BarChart>
                                    </ResponsiveContainer>
                                )}
                            </div>
                        </div>
                    </div>
                </>
            )}
        </div>
    );
};