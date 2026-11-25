import { t } from '@/i18n'

export type TransactionKind = 'income' | 'expense'
export type OperationKind = TransactionKind | 'openingBalance' | 'transferDebit' | 'transferCredit'
export type AccountType = 'cash' | 'bank' | 'savings' | 'deposit' | 'investment' | 'liability'

export function accountTypeLabel(type: AccountType): string {
  return t(`finance.accountTypes.${type}`)
}
export interface BudgetSpace { id: string; name: string; isActive: boolean }
export interface Account { id: string; budgetSpaceId: string; budgetSpaceName?: string; name: string; type: AccountType; currency: string; balance: number; isActive: boolean }
export interface Category { id: string; budgetSpaceId: string; name: string; appliesTo: TransactionKind; parentCategoryId?: string | null; isActive: boolean; sortOrder: number }
export interface Transaction { id: string; accountId: string; categoryId?: string | null; kind: OperationKind; amount: number; occurredOn: string; description?: string; accountName?: string; categoryName?: string }
export interface ReportCategory { kind: TransactionKind; categoryId: string; category: string; total: number; count: number }
export interface MonthlyReport { year: number; month: number; income: number; expenses: number; categories: ReportCategory[] }
export interface DateRangeReport { from: string; to: string; income: number; expenses: number; categories: ReportCategory[] }
export interface Dashboard { totalBalance: number; incomeThisMonth: number; expenseThisMonth: number; accounts: Account[]; recentTransactions: Transaction[] }
