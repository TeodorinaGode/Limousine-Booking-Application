export interface BarChartSeries {
  name: string;
  color: string;
}

export interface BarChartDatum {
  label: string;
  values: number[];
}

interface BarChartProps {
  series: BarChartSeries[];
  data: BarChartDatum[];
  height?: number;
  formatValue?: (value: number) => string;
}

/**
 * A small dependency-free inline-SVG bar chart — the project has no chart
 * library yet (section 30), and grouped bars for a handful of daily series is
 * simple enough not to justify adding one.
 */
function BarChart({ series, data, height = 220, formatValue }: BarChartProps) {
  if (data.length === 0) {
    return <p>No data available for the selected period.</p>;
  }

  const maxValue = Math.max(1, ...data.flatMap((d) => d.values));
  const chartWidth = Math.max(400, data.length * 60);
  const barGroupWidth = chartWidth / data.length;
  const barWidth = Math.min(24, (barGroupWidth - 8) / series.length);
  const format = formatValue ?? ((v: number) => String(v));

  return (
    <div style={{ overflowX: "auto" }}>
      {series.length > 1 && (
        <div style={{ display: "flex", gap: "1rem", marginBottom: "0.5rem", fontSize: "0.85rem" }}>
          {series.map((s) => (
            <span key={s.name} style={{ display: "inline-flex", alignItems: "center", gap: "0.25rem" }}>
              <span style={{ width: "10px", height: "10px", background: s.color, display: "inline-block", borderRadius: "2px" }} />
              {s.name}
            </span>
          ))}
        </div>
      )}
      <svg width={chartWidth} height={height} role="img" aria-label="Chart">
        {data.map((datum, i) => {
          const groupX = i * barGroupWidth;
          return (
            <g key={datum.label}>
              {datum.values.map((value, seriesIndex) => {
                const barHeight = (value / maxValue) * (height - 40);
                const x = groupX + 4 + seriesIndex * barWidth;
                const y = height - 24 - barHeight;
                return (
                  <g key={seriesIndex}>
                    <title>{`${datum.label}: ${format(value)}`}</title>
                    <rect x={x} y={y} width={barWidth - 2} height={barHeight} fill={series[seriesIndex]?.color ?? "#4a6fa5"} />
                  </g>
                );
              })}
              <text x={groupX + barGroupWidth / 2 - 4} y={height - 6} fontSize="10" textAnchor="middle">
                {datum.label}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}

export default BarChart;
