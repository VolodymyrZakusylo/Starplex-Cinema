export interface RevenueChartItem {
  date: string;
  revenue: number;
  ticketsCount: number;
}

export interface MoviePopularity {
  movieTitle: string;
  ticketsSold: number;
  earnings: number;
}

export interface SeatTypeBreakdown {
  type: string;
  count: number;
  revenue: number;
}

export interface AdminStatsDto {
  totalRevenue: number;
  totalTicketsSold: number;
  activeSessionsCount: number;
  refundedAmount: number;
  averageOrderValue: number;
  revenueChart: RevenueChartItem[];
  topMovies: MoviePopularity[];
  seatTypeStats: SeatTypeBreakdown[];
}

export interface UserStaffDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  currentRole: string;
  cinemaId?: string | null;
  cinemaName?: string | null;
}

export interface PagedUserStaffResponse {
  users: UserStaffDto[];
  totalCount: number;
}

export interface AuditLogDto {
  id: string;
  userId: string;
  userEmail: string;
  action: string;
  entityName: string;
  entityId: string;
  timestamp: string;
  details?: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
