import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import PaymentStatusPage from "./PaymentStatusPage";
import * as paymentService from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";
import type { PublicPaymentStatusDto } from "../../types/payment";

vi.mock("../../services/paymentService");

const mockedPaymentService = vi.mocked(paymentService);

function makeStatus(overrides: Partial<PublicPaymentStatusDto> = {}): PublicPaymentStatusDto {
  return {
    bookingReference: "LM-20261225-123456",
    status: "Paid",
    amount: 180,
    currency: "CHF",
    paidAt: "2026-08-19T10:00:00Z",
    ...overrides,
  };
}

function renderPage(path = "/booking/payment/LM-20261225-123456?token=abc-token") {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/booking/payment/:bookingReference" element={<PaymentStatusPage />} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe("PaymentStatusPage", () => {
  it("shows the current payment status", async () => {
    mockedPaymentService.getPaymentStatus.mockResolvedValue(makeStatus());

    renderPage();

    expect(await screen.findByText("Paid")).toBeInTheDocument();
    expect(mockedPaymentService.getPaymentStatus).toHaveBeenCalledWith("LM-20261225-123456", "abc-token");
  });

  it("offers a Pay Now button when no payment has started yet", async () => {
    mockedPaymentService.getPaymentStatus.mockRejectedValue(new ApiError(404, "No payment found for this booking."));

    renderPage();

    expect(await screen.findByRole("button", { name: "Pay Now" })).toBeInTheDocument();
  });

  it("offers a Retry Payment button for a failed payment", async () => {
    mockedPaymentService.getPaymentStatus.mockResolvedValue(makeStatus({ status: "Failed", paidAt: null }));

    renderPage();

    expect(await screen.findByRole("button", { name: "Retry Payment" })).toBeInTheDocument();
  });

  it("does not offer a retry button for a Paid payment", async () => {
    mockedPaymentService.getPaymentStatus.mockResolvedValue(makeStatus({ status: "Paid" }));

    renderPage();

    await screen.findByText("Paid");
    expect(screen.queryByRole("button", { name: "Retry Payment" })).not.toBeInTheDocument();
  });

  it("redirects to checkout when retrying a failed payment", async () => {
    mockedPaymentService.getPaymentStatus.mockResolvedValue(makeStatus({ status: "Cancelled", paidAt: null }));
    mockedPaymentService.retryPayment.mockResolvedValue({
      paymentId: "p2",
      checkoutUrl: "https://checkout.example/cs_retry",
      expiresAt: "2026-08-19T11:00:00Z",
    });
    const user = userEvent.setup();

    renderPage();
    await user.click(await screen.findByRole("button", { name: "Retry Payment" }));

    await waitFor(() => expect(mockedPaymentService.retryPayment).toHaveBeenCalledWith("LM-20261225-123456", "abc-token"));
  });

  it("shows an error when the link is missing the access token", async () => {
    renderPage("/booking/payment/LM-20261225-123456");

    expect(await screen.findByRole("alert")).toHaveTextContent("This payment link is missing required information.");
    expect(mockedPaymentService.getPaymentStatus).not.toHaveBeenCalled();
  });
});
