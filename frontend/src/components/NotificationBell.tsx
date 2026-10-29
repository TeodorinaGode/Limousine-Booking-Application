import { useCallback, useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { getAdminAlertsSummary, markBookingsAlertsSeen } from "../services/adminAlertsService";
import type { AdminAlertsSummaryDto } from "../types/adminAlerts";

const POLL_INTERVAL_MS = 25_000;

function formatRelativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.max(0, Math.round(diffMs / 60_000));
  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.round(hours / 24)}d ago`;
}

/** Sidebar notification bell (section: admin alerts) — polls new-bookings and unread-CRM-email counts every ~25s. Opening the dropdown marks bookings seen (resets that count); unread emails only clear when the thread itself is opened in the Inbox, so the bell never fakes that as read. */
function NotificationBell({ isCollapsed }: { isCollapsed: boolean }) {
  const { accessToken } = useAuth();
  const [summary, setSummary] = useState<AdminAlertsSummaryDto | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const loadSummary = useCallback(async () => {
    if (!accessToken) return;
    try {
      setSummary(await getAdminAlertsSummary(accessToken));
    } catch {
      // The bell is a convenience indicator — a failed poll just tries again next interval.
    }
  }, [accessToken]);

  useEffect(() => {
    loadSummary();
    const interval = setInterval(loadSummary, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [loadSummary]);

  useEffect(() => {
    if (!isOpen) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setIsOpen(false);
    };

    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [isOpen]);

  const handleToggle = async () => {
    const opening = !isOpen;
    setIsOpen(opening);

    if (opening && accessToken && summary && summary.newBookingsCount > 0) {
      try {
        await markBookingsAlertsSeen(accessToken);
        await loadSummary();
      } catch {
        // Non-fatal — the badge just stays as-is until the next successful poll.
      }
    }
  };

  const totalUnread = (summary?.newBookingsCount ?? 0) + (summary?.unreadEmailsCount ?? 0);

  return (
    <div className="notification-bell" ref={containerRef}>
      <button
        type="button"
        className="notification-bell__trigger"
        aria-haspopup="true"
        aria-expanded={isOpen}
        aria-label={totalUnread > 0 ? `Notifications, ${totalUnread} unread` : "Notifications"}
        title={isCollapsed ? "Notifications" : undefined}
        onClick={handleToggle}
      >
        <span aria-hidden="true">🔔</span>
        {!isCollapsed && <span>Notifications</span>}
        {totalUnread > 0 && <span className="notification-bell__badge">{totalUnread > 99 ? "99+" : totalUnread}</span>}
      </button>

      {isOpen && (
        <div className="notification-bell__menu" role="menu">
          <section className="notification-bell__section">
            <h3>New Bookings {summary && summary.newBookingsCount > 0 && `(${summary.newBookingsCount})`}</h3>
            {!summary || summary.recentBookings.length === 0 ? (
              <p className="notification-bell__empty">No recent bookings.</p>
            ) : (
              <ul>
                {summary.recentBookings.map((booking) => (
                  <li key={booking.id}>
                    <Link to={`/admin/bookings/${booking.id}`} onClick={() => setIsOpen(false)} className={booking.isNew ? "notification-bell__item--new" : undefined}>
                      <span>{booking.bookingReference} — {booking.customerName}</span>
                      <span className="notification-bell__time">{formatRelativeTime(booking.createdAt)}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="notification-bell__section">
            <h3>Unread Emails {summary && summary.unreadEmailsCount > 0 && `(${summary.unreadEmailsCount})`}</h3>
            {!summary || summary.recentUnreadEmailThreads.length === 0 ? (
              <p className="notification-bell__empty">No unread emails.</p>
            ) : (
              <ul>
                {summary.recentUnreadEmailThreads.map((thread) => (
                  <li key={thread.id}>
                    <Link to={`/admin/crm/inbox?thread=${thread.id}`} onClick={() => setIsOpen(false)}>
                      <span>{thread.subject}{thread.fromName ? ` — ${thread.fromName}` : ""}</span>
                      <span className="notification-bell__time">{formatRelativeTime(thread.lastMessageAt)}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>
      )}
    </div>
  );
}

export default NotificationBell;
