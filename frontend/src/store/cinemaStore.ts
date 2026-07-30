import { create } from 'zustand';

interface CinemaStoreInterface {
  selectedCinemaId: string;
  setCinemaId: (id: string) => void;
}

export const useCinemaStore = create<CinemaStoreInterface>((set) => ({
  selectedCinemaId: localStorage.getItem('selectedCinemaId') || '',

  setCinemaId: (id: string) => {
    localStorage.setItem('selectedCinemaId', id);
    set({ selectedCinemaId: id });
  },
}));