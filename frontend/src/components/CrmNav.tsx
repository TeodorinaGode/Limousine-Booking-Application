import { Link, useLocation } from "react-router-dom";

/**
 * Secondary tab bar for the CRM section, rendered at the top of every CRM
 * page. AdminNav itself gets only one "CRM" entry (it's flat with no other
 * sub-nav precedent) — this is where the CRM sections actually live.
 * Icons speed up scanning across 8 tabs, and the active tab gets its own
 * underline (not just a border-button swap) so it stays legible even in a
 * row this long. Tabs appear here only once their page exists (phased rollout).
 */
function CrmNav() {
  const location = useLocation();

  const tabs = [
    { to: "/admin/crm", label: "Overview", icon: "📊" },
    { to: "/admin/crm/companies", label: "Companies", icon: "🏢" },
    { to: "/admin/crm/contacts", label: "Contacts", icon: "👤" },
    { to: "/admin/crm/deals", label: "Deals", icon: "🤝" },
    { to: "/admin/crm/activities", label: "Activities", icon: "🕒" },
    { to: "/admin/crm/tasks", label: "Tasks", icon: "✅" },
    { to: "/admin/crm/reports", label: "Reports", icon: "📈" },
    { to: "/admin/crm/inbox", label: "Inbox", icon: "✉️" },
  ];

  return (
    <nav className="crm-nav" aria-label="CRM navigation">
      {tabs.map((tab) => {
        const isActive =
          location.pathname === tab.to || (tab.to !== "/admin/crm" && location.pathname.startsWith(`${tab.to}/`));
        return (
          <Link
            key={tab.to}
            to={tab.to}
            className={`crm-nav__tab${isActive ? " crm-nav__tab--active" : ""}`}
            aria-current={isActive ? "page" : undefined}
          >
            <span aria-hidden="true" className="crm-nav__icon">{tab.icon}</span>
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}

export default CrmNav;
