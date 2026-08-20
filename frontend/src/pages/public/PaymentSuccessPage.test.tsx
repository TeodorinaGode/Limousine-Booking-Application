import { render, screen } from "@testing-library/react";
import { act } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import PaymentSuccessPage from "./PaymentSuccessPage";
import * as paymentService from "../../services/paymentService";
import type { PublicPaymentStatusDto } from "../../types/payment";

vi.mock("../../services/paymentService");

const mockedPaymentService = vi.mocked(paymentService);

function makeStatus(overrides: Partial<PublicPaymentStatusDto> = {}): PublicPaymentStatusDto {
  return {
    bookingReference: "LM-20261225-123456",
    status: "Pending",
    amount: 180,
    currency: "CHF",
    paidAt: null,
    ...overrides,
  };
}

function renderPage(query = "?ref=LM-20261225-123456&token=abc-token") {
  return render(
    <MemoryRouter initialEntries={[`/booking/payment/success${query}`]}>
      <Routes>
        <Route path="/booking/payment/success" element={<PaymentSuccessPage />} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

afterEach(() => {
  vi.useRealTimers();
});

describe("PaymentSuccessPage", () => {
  it("shows a confirming state while the payment is still Pending", async () => {
    mockedPaymentService.getPaymentStatus.mockResolvedValue(makeStatus({ status: "Pending" }));

    renderPage();

    expect(await screen.findByText("Please wait while we confirm your payment...")).toBeInTheDocument();
  });

  it("shows a success message once the payment becomes Paid", async () => {
    mockedPaymentService.getPaymentStatus.mockResolvedValue(makeStatus({ status: "Paid", paidAt: "2026-08-19T10:00:00Z" }));

    renderPage();

    expect(await screen.findByText("Payment Successful")).toBeInTheDocument();
    expect(screen.getByText(/180\.00 CHF/)).toBeInTheDocument();
  });

  it("stops polling and offers a status link after enough attempts without confirmation", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    mockedPaymentService.getPaymentStatus.mockResolvedValue(makeStatus({ status: "Pending" }));

    renderPage();

    // 15 attempts at a 2s interval — advance well past that so polling gives up.
    await act(async () => {
      await vi.advanceTimersByTimeAsync(2000 * 16);
    });

    expect(screen.getByText(/still waiting for confirmation/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Check payment status" })).toBeInTheDocument();
  });

  it("shows an error when the link is missing required information", async () => {
    renderPage("");

    expect(await screen.findByRole("alert")).toHaveTextContent("This payment link is missing required information.");
    expect(mockedPaymentService.getPaymentStatus).not.toHaveBeenCalled();
  });
});
