"use client"

import { Button } from "@core/components/ui/button"
import { SimpleTooltip } from "./simple-tooltip"
import { useMobile } from "@core/hooks/use-mobile"
import {
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
} from "lucide-react"

interface PaginationProps {
  page: number
  pages: number
  total: number
  limit: number
  onPageChange: (page: number) => void
  showInfo?: boolean
  itemLabel?: string
}

export function Pagination({
  page,
  pages,
  total,
  limit,
  onPageChange,
  showInfo = true,
  itemLabel = "resultado",
}: PaginationProps) {
  const isMobile = useMobile()

  if (pages <= 1) return null

  const maxVisiblePages = isMobile ? 3 : 5

  const getPageNumbers = () => {
    const pageNumbers: number[] = []

    if (pages <= maxVisiblePages) {
      for (let i = 1; i <= pages; i++) pageNumbers.push(i)
    } else if (page <= Math.floor(maxVisiblePages / 2) + 1) {
      for (let i = 1; i <= maxVisiblePages; i++) pageNumbers.push(i)
    } else if (page >= pages - Math.floor(maxVisiblePages / 2)) {
      for (let i = pages - maxVisiblePages + 1; i <= pages; i++)
        pageNumbers.push(i)
    } else {
      for (
        let i = page - Math.floor(maxVisiblePages / 2);
        i <= page + Math.floor(maxVisiblePages / 2);
        i++
      )
        pageNumbers.push(i)
    }

    return pageNumbers
  }

  const pluralLabel = total !== 1 ? `${itemLabel}s` : itemLabel

  return (
    <div
      role="navigation"
      aria-label="Paginação"
      className="flex flex-col sm:flex-row items-center justify-between gap-4"
    >
      {showInfo && (
        <div className="text-sm text-muted-foreground order-2 sm:order-1">
          <span className="hidden sm:inline">
            Mostrando {(page - 1) * limit + 1} a {Math.min(page * limit, total)}{" "}
            de {total} {pluralLabel}
          </span>
          <span className="sm:hidden">
            {page} de {pages}
          </span>
        </div>
      )}

      <div className="flex items-center gap-1 sm:gap-2 order-1 sm:order-2">
        <SimpleTooltip label="Primeira página">
          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageChange(1)}
            disabled={page === 1}
            className="h-8 w-8 sm:h-9 sm:w-9"
          >
            <ChevronsLeft className="h-4 w-4" />
            <span className="sr-only">Primeira página</span>
          </Button>
        </SimpleTooltip>
        <SimpleTooltip label="Página anterior">
          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageChange(page - 1)}
            disabled={page === 1}
            className="h-8 w-8 sm:h-9 sm:w-9"
          >
            <ChevronLeft className="h-4 w-4" />
            <span className="sr-only">Página anterior</span>
          </Button>
        </SimpleTooltip>

        <div className="flex items-center gap-1">
          {getPageNumbers().map((pageNum) => (
            <Button
              key={pageNum}
              variant={pageNum === page ? "default" : "outline"}
              size="icon"
              onClick={() => onPageChange(pageNum)}
              className="h-8 w-8 sm:h-9 sm:w-9"
              aria-current={pageNum === page ? "page" : undefined}
              aria-label={`Página ${pageNum}`}
            >
              {pageNum}
            </Button>
          ))}
        </div>

        <SimpleTooltip label="Próxima página">
          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageChange(page + 1)}
            disabled={page === pages}
            className="h-8 w-8 sm:h-9 sm:w-9"
          >
            <ChevronRight className="h-4 w-4" />
            <span className="sr-only">Próxima página</span>
          </Button>
        </SimpleTooltip>
        <SimpleTooltip label="Última página">
          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageChange(pages)}
            disabled={page === pages}
            className="h-8 w-8 sm:h-9 sm:w-9"
          >
            <ChevronsRight className="h-4 w-4" />
            <span className="sr-only">Última página</span>
          </Button>
        </SimpleTooltip>
      </div>
    </div>
  )
}
