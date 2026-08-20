import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import PaymentCancelledPage from "./PaymentCancelledPage";
import * as paymentService from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";

vi.mock("../../services/paymentService");

const mockedPaymentService = vi.mocked(paymentService);

function renderPage(query = "?ref=LM-20261225-123456&token=abc-token") {
  return render(
    <MemoryRouter initialEntries={[`/booking/payment/cancelled${query}`]}>
      <Routes>
        <Route path="/booking/payment/cancelled" element={<PaymentCancelledPage />} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe("PaymentCancelledPage", () => {
  it("shows the cancelled message", () => {
    renderPage();

    expect(screen.getByText("Payment Cancelled")).toBeInTheDocument();
    expect(screen.getByText("Your payment was not completed. Your booking has not been charged.")).toBeInTheDocument();
  });

  it("retries payment via the retry endpoint", async () => {
    mockedPaymentService.retryPayment.mockResolvedValue({
      paymentId: "p3",
      checkoutUrl: "https://checkout.example/cs_retry",
      expiresAt: "2026-08-19T11:00:00Z",
    });
    const user = userEvent.setup();

    renderPage();
    await user.click(screen.getByRole("button", { name: "Try Again" }));

    await waitFor(() => expect(mockedPaymentService.retryPayment).toHaveBeenCalledWith("LM-20261225-123456", "abc-token"));
  });

  it("shows an error when retrying fails", async () => {
    mockedPaymentService.retryPayment.mockRejectedValue(new ApiError(409, "This booking has already been paid."));
    const user = userEvent.setup();

    renderPage();
    await user.click(screen.getByRole("button", { name: "Try Again" }));

    expect(await screen.findByText("This booking has already been paid.")).toBeInTheDocument();
  });

  it("hides the retry button when the link is missing required information", () => {
    renderPage("");

    expect(screen.queryByRole("button", { name: "Try Again" })).not.toBeInTheDocument();
  });
});
