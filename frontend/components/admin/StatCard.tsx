export function StatCard({
  label,
  value,
  sub,
  icon,
}: {
  label: string;
  value: string | number;
  sub?: string;
  icon?: React.ReactNode;
}) {
  return (
    <div className="admin-stat-card">
      <div className="admin-stat-header">
        <div className="admin-stat-label">{label}</div>
        {icon && <div className="admin-stat-icon">{icon}</div>}
      </div>
      <div className="admin-stat-value">{value}</div>
      {sub && <div className="admin-stat-sub">{sub}</div>}
    </div>
  );
}
