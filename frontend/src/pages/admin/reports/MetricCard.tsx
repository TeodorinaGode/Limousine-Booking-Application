const cardStyle: React.CSSProperties = {
  border: "1px solid #ccc",
  borderRadius: "8px",
  padding: "1rem",
  minWidth: "160px",
  flex: "1 1 160px",
};

function MetricCard({ label, value }: { label: string; value: string | number }) {
  return (
    <div style={cardStyle}>
      <div style={{ fontSize: "0.85rem", color: "#555" }}>{label}</div>
      <div style={{ fontSize: "1.6rem", fontWeight: 600 }}>{value}</div>
    </div>
  );
}

export default MetricCard;
