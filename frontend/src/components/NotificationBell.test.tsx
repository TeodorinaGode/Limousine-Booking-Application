import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import NotificationBell from "./NotificationBell";
import * as adminAlertsService from "../services/adminAlertsService";
import * as authContext from "../context/AuthContext";
import type { AdminAlertsSummaryDto } from "../types/adminAlerts";

vi.mock("../services/adminAlertsService");
vi.mock("../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedAlertsService = vi.mocked(adminAlertsService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeSummary(overrides: Partial<AdminAlertsSummaryDto> = {}): AdminAlertsSummaryDto {
  return {
    newBookingsCount: 0,
    recentBookings: [],
    unreadEmailsCount: 0,
    recentUnreadEmailThreads: [],
    ...overrides,
  };
}

function renderBell() {
  return render(
    <MemoryRouter>
      <NotificationBell isCollapsed={false} />
    </MemoryRouter>
  );
}

describe("NotificationBell", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockedUseAuth.mockReturnValue({
      accessToken: "test-token",
      user: null,
      expiresAt: null,
      isAuthenticated: true,
      login: vi.fn(),
      logout: vi.fn(),
    });
    mockedAlertsService.markBookingsAlertsSeen.mockResolvedValue(undefined);
  });

  it("shows no badge when there is nothing new", async () => {
    mockedAlertsService.getAdminAlertsSummary.mockResolvedValue(makeSummary());
    renderBell();

    await waitFor(() => expect(mockedAlertsService.getAdminAlertsSummary).toHaveBeenCalled());
    expect(screen.queryByText(/^\d+$/)).not.toBeInTheDocument();
  });

  it("shows a combined badge count of new bookings and unread emails", async () => {
    mockedAlertsService.getAdminAlertsSummary.mockResolvedValue(makeSummary({ newBookingsCount: 2, unreadEmailsCount: 3 }));
    renderBell();

    expect(await screen.findByText("5")).toBeInTheDocument();
  });

  it("opens the dropdown listing recent bookings and unread email threads", async () => {
    mockedAlertsService.getAdminAlertsSummary.mockResolvedValue(
      makeSummary({
        newBookingsCount: 1,
        recentBookings: [{ id: "b1", bookingReference: "LM-1", customerName: "Jane Doe", createdAt: new Date().toISOString(), isNew: true }],
        unreadEmailsCount: 1,
        recentUnreadEmailThreads: [{ id: "t1", subject: "Quote request", fromName: "Acme Corp", lastMessageAt: new Date().toISOString() }],
      })
    );
    const user = userEvent.setup();
    renderBell();
    await screen.findByText("2");

    await user.click(screen.getByRole("button", { name: /notifications/i }));

    expect(screen.getByText(/LM-1 — Jane Doe/)).toBeInTheDocument();
    expect(screen.getByText(/Quote request — Acme Corp/)).toBeInTheDocument();
  });

  it("marks bookings as seen when opened with a nonzero new-bookings count", async () => {
    mockedAlertsService.getAdminAlertsSummary.mockResolvedValue(makeSummary({ newBookingsCount: 1 }));
    const user = userEvent.setup();
    renderBell();
    await screen.findByText("1");

    await user.click(screen.getByRole("button", { name: /notifications/i }));

    await waitFor(() => expect(mockedAlertsService.markBookingsAlertsSeen).toHaveBeenCalledWith("test-token"));
  });

  it("does not call markBookingsAlertsSeen when there are no new bookings", async () => {
    mockedAlertsService.getAdminAlertsSummary.mockResolvedValue(makeSummary({ unreadEmailsCount: 1 }));
    const user = userEvent.setup();
    renderBell();
    await screen.findByText("1");

    await user.click(screen.getByRole("button", { name: /notifications/i }));

    expect(mockedAlertsService.markBookingsAlertsSeen).not.toHaveBeenCalled();
  });

  it("closes the dropdown when pressing Escape", async () => {
    mockedAlertsService.getAdminAlertsSummary.mockResolvedValue(makeSummary());
    const user = userEvent.setup();
    renderBell();
    await waitFor(() => expect(mockedAlertsService.getAdminAlertsSummary).toHaveBeenCalled());

    await user.click(screen.getByRole("button", { name: /notifications/i }));
    expect(screen.getByRole("menu")).toBeInTheDocument();

    await user.keyboard("{Escape}");
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
  });
});
