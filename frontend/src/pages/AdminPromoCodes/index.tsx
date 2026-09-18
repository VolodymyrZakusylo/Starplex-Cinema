import React, { useEffect, useState } from 'react';
import { Ticket, Plus, Trash2, Calendar, RefreshCw } from 'lucide-react';
import { discountsApi } from '@/api/discounts';
import { getApiErrorMessage } from '@/api/errors';
import { useToast } from '@/hooks/useToast';
import type { DiscountDto } from '@/types/discounts';

export const AdminPromoCodesPage: React.FC = () => {
  const { confirm, showError, showSuccess } = useToast();
  const [promos, setPromos] = useState<DiscountDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [percentage, setPercentage] = useState<number>(10);
  const [usageLimit, setUsageLimit] = useState<number>(100);
  const [validFrom, setValidFrom] = useState('');
  const [validTo, setValidTo] = useState('');

  const fetchPromoCodes = async () => {
    setIsLoading(true);
    try {
      const data = await discountsApi.getAll();
      setPromos(data);
    } catch (err: any) {
      showError(err.response?.data?.message || 'Не вдалося завантажити перелік промокодів.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchPromoCodes();
    
    const today = new Date().toISOString().split('T')[0];
    const nextMonth = new Date();
    nextMonth.setMonth(nextMonth.getMonth() + 1);
    
    setValidFrom(today);
    setValidTo(nextMonth.toISOString().split('T')[0]);
  }, []);

  const handleCreatePromo = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code || !name) return;

    try {
      await discountsApi.create({
        code: code.toUpperCase().trim(),
        name: name.trim(),
        percentage,
        usageLimit,
        validFrom: new Date(validFrom).toISOString(),
        validTo: new Date(validTo).toISOString(),
        isActive: true
      });

      setCode('');
      setName('');
      fetchPromoCodes();
      showSuccess(`🎉 Промокод ${code.toUpperCase().trim()} успішно згенеровано!`);
    } catch (err: any) {
      showError(err.response?.data?.message || err.response?.data?.Message || 'Не вдалося створити промокод.');
    }
  };

  const handleDeletePromo = (id: string) => {
    confirm('Видалити промокод? Якщо він пов’язаний із бронюваннями, його буде деактивовано зі збереженням історії.', async () => {
      try {
        const result = await discountsApi.delete(id);
        showSuccess(result.outcome === 'Deleted' ? 'Промокод успішно видалено.' : 'Промокод деактивовано, оскільки він має історію використання.');
        fetchPromoCodes();
      } catch (err) {
        showError(getApiErrorMessage(err, 'Не вдалося видалити промокод.'));
      }
    });
  };

  return (
    <div className="w-full select-none grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
      
      <div className="lg:col-span-1 bg-dark-secondary border border-white/5 p-6 rounded-3xl shadow-2xl flex flex-col gap-6">
        <div>
          <h2 className="text-xl font-bold tracking-tight flex items-center gap-2 text-[#ffbd14]">
            <Plus className="w-5 h-5" /> Генерація промокоду
          </h2>
          <p className="text-xs text-gray-400 mt-1">Створення нових знижок для клієнтів StarPlex</p>
        </div>

        <form onSubmit={handleCreatePromo} className="flex flex-col gap-4 text-xs">
          <div>
            <label className="block text-gray-400 font-semibold mb-1.5 uppercase">Промокод (Унікальний)</label>
            <input
              type="text"
              placeholder="НАПР. STAR20"
              value={code}
              onChange={(e) => setCode(e.target.value.toUpperCase())}
              className="w-full bg-[#161720] border border-white/10 rounded-xl px-4 py-2.5 text-white font-bold tracking-wider uppercase focus:outline-none focus:border-[#ffbd14]"
              required
            />
          </div>

          <div>
            <label className="block text-gray-400 font-semibold mb-1.5 uppercase">Назва маркетингової акції</label>
            <input
              type="text"
              placeholder="Напр. Знижка до відкриття залу"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full bg-[#161720] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-[#ffbd14]"
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-gray-400 font-semibold mb-1.5 uppercase">Знижка (%)</label>
              <input
                type="number"
                min="1"
                max="100"
                value={percentage}
                onChange={(e) => setPercentage(Number(e.target.value))}
                className="w-full bg-[#161720] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-[#ffbd14] font-bold"
                required
              />
            </div>
            <div>
              <label className="block text-gray-400 font-semibold mb-1.5 uppercase">Ліміт активацій</label>
              <input
                type="number"
                min="1"
                value={usageLimit}
                onChange={(e) => setUsageLimit(Number(e.target.value))}
                className="w-full bg-[#161720] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-[#ffbd14] font-bold"
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-gray-400 font-semibold mb-1.5 uppercase">Діє з</label>
              <input
                type="date"
                value={validFrom}
                onChange={(e) => setValidFrom(e.target.value)}
                className="w-full bg-[#161720] border border-white/10 rounded-xl px-3 py-2.5 text-white focus:outline-none focus:border-[#ffbd14]"
                required
              />
            </div>
            <div>
              <label className="block text-gray-400 font-semibold mb-1.5 uppercase">Діє до</label>
              <input
                type="date"
                value={validTo}
                onChange={(e) => setValidTo(e.target.value)}
                className="w-full bg-[#161720] border border-white/10 rounded-xl px-3 py-2.5 text-white focus:outline-none focus:border-[#ffbd14]"
                required
              />
            </div>
          </div>

          <button
            type="submit"
            className="w-full bg-[#ffbd14] hover:bg-[#e0a410] text-black font-black py-3 rounded-xl transition-all shadow-lg uppercase tracking-wider mt-2 cursor-pointer border-none shadow-[#ffbd14]/5"
          >
            Зберегти промокод
          </button>
        </form>
      </div>

      <div className="lg:col-span-2 flex flex-col gap-4">
        <div className="flex justify-between items-center border-b border-white/5 pb-4">
          <div>
            <h1 className="text-2xl font-black tracking-tight flex items-center gap-2">
              <Ticket className="text-[#ffbd14] w-6 h-6" /> Активні промокоди
            </h1>
            <p className="text-gray-400 text-xs mt-0.5">Керування та моніторинг запущених знижкових кампаній</p>
          </div>
          <button
            type="button"
            onClick={fetchPromoCodes}
            className="p-2.5 bg-dark-secondary border border-white/5 rounded-xl hover:border-[#ffbd14] text-white transition-all cursor-pointer"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin text-[#ffbd14]' : ''}`} />
          </button>
        </div>

        {isLoading ? (
          <div className="flex items-center justify-center py-40">
            <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
          </div>
        ) : promos.length === 0 ? (
          <div className="text-center py-20 bg-dark-secondary border border-white/5 rounded-3xl">
            <p className="text-gray-500 italic text-xs">Активних промокодів у системі не знайдено.</p>
          </div>
        ) : (
          <div className="w-full overflow-x-auto rounded-2xl border border-white/5 bg-dark-secondary shadow-2xl">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="border-b border-white/5 bg-white/[0.02] text-gray-400 font-bold uppercase tracking-wider text-[10px]">
                  <th className="p-4">Код</th>
                  <th className="p-4">Назва кампанії</th>
                  <th className="p-4 text-center">Знижка</th>
                  <th className="p-4 text-center">Використано</th>
                  <th className="p-4"><Calendar className="w-3.5 h-3.5 inline mr-1" /> Термін дії</th>
                  <th className="p-4 text-center">Статус</th>
                  <th className="p-4 text-center">Дія</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/[0.02]">
                {promos.map((promo) => {
                  const isExpired = new Date(promo.validTo) < new Date();
                  const isLimitReached = promo.usageCount >= promo.usageLimit;
                  
                  return (
                    <tr key={promo.id} className="hover:bg-white/[0.01] transition-colors">
                      <td className="p-4 font-mono font-black text-[#ffbd14] text-sm tracking-wider">
                        {promo.code}
                      </td>
                      <td className="p-4 font-medium text-gray-200">{promo.name}</td>
                      <td className="p-4 text-center font-black text-sm text-emerald-400">
                        {promo.percentage}%
                      </td>
                      <td className="p-4 text-center font-mono text-gray-300">
                        <span className={isLimitReached ? 'text-red-400 font-bold' : 'text-white'}>
                          {promo.usageCount}
                        </span>
                        <span className="text-gray-600"> / {promo.usageLimit}</span>
                      </td>
                      <td className="p-4 text-gray-400">
                        {new Date(promo.validFrom).toLocaleDateString('uk-UA')} - {new Date(promo.validTo).toLocaleDateString('uk-UA')}
                      </td>
                      <td className="p-4 text-center">
                        {promo.isActive && !isExpired && !isLimitReached ? (
                          <span className="bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 px-2 py-0.5 rounded-full uppercase text-[9px] font-bold tracking-wider">
                            Активний
                          </span>
                        ) : (
                          <span className="bg-red-500/10 border border-red-500/20 text-red-400 px-2 py-0.5 rounded-full uppercase text-[9px] font-bold tracking-wider">
                            {isExpired ? 'Протерміновано' : isLimitReached ? 'Вичерпано' : 'Вимкнено'}
                          </span>
                        )}
                      </td>
                      <td className="p-4 text-center">
                        <button
                          type="button"
                          onClick={() => handleDeletePromo(promo.id)}
                          className="p-2 hover:bg-red-500/10 text-gray-500 hover:text-red-400 rounded-xl transition-all cursor-pointer border-none bg-transparent"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

    </div>
  );
};
