export type BarChartDatum = Readonly<{
  label: string
  value: number
}>

export type BarChartProps = Readonly<{
  title?: string
  data?: readonly BarChartDatum[]
  className?: string
}>
