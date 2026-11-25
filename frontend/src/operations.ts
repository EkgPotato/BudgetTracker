import { t } from '@/i18n'
import type { OperationKind } from '@/types'
import '@/locales/operationMessages'

export function isDebit(kind: OperationKind): boolean {
  return kind === 'expense' || kind === 'transferDebit'
}

export function operationLabel(kind: OperationKind): string {
  if (kind === 'transferDebit' || kind === 'transferCredit' || kind === 'openingBalance') return t(`operations.${kind}`)
  return t(`finance.transactions.${kind}`)
}
