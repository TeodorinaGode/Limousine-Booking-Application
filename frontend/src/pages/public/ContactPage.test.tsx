import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ContactPage from "./ContactPage";
import { AuthProvider } from "../../context/AuthContext";
import * as contactService from "../../services/contactService";
import * as companyService from "../../services/companyService";
import { ApiError } from "../../services/apiClient";

vi.mock("../../services/contactService");
vi.mock("../../services/companyService");

const mockedContactService = vi.mocked(contactService);
const mockedCompanyService = vi.mocked(companyService);

function renderPage() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <ContactPage />
      </AuthProvider>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedCompanyService.getCompanyInfo.mockResolvedValue({
    companyName: "Test Chauffeur",
    tagline: "Test Tagline",
    phone: "+41 79 000 00 00",
    email: "info@example.com",
    address: "Bahnhofplatz 1, Basel",
    website: "",
    openingHours: "",
    emergencyPhone: null,
    description: null,
  });
});

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText("Name"), "Jane Doe");
  await user.type(screen.getByLabelText("Email"), "jane@example.com");
  await user.type(screen.getByLabelText("Subject"), "Question about a booking");
  await user.type(screen.getByLabelText("Message"), "I would like more information about your airport transfer service.");
}

describe("ContactPage", () => {
  it("shows validation errors when required fields are missing", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByRole("button", { name: "Send Message" }));

    expect(await screen.findAllByText("This field is required.")).not.toHaveLength(0);
    expect(mockedContactService.submitContactForm).not.toHaveBeenCalled();
  });

  it("rejects an invalid email address", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.type(screen.getByLabelText("Name"), "Jane Doe");
    await user.type(screen.getByLabelText("Email"), "not-an-email");
    await user.type(screen.getByLabelText("Subject"), "Question");
    await user.type(screen.getByLabelText("Message"), "I would like more information please.");
    await user.click(screen.getByRole("button", { name: "Send Message" }));

    expect(mockedContactService.submitContactForm).not.toHaveBeenCalled();
  });

  it("submits the form and shows a success message", async () => {
    const user = userEvent.setup();
    mockedContactService.submitContactForm.mockResolvedValue({ message: "ok" });
    renderPage();

    await fillValidForm(user);
    await user.click(screen.getByRole("button", { name: "Send Message" }));

    expect(await screen.findByRole("status")).toHaveTextContent("Thank you — your message has been received.");
    expect(mockedContactService.submitContactForm).toHaveBeenCalledWith(
      expect.objectContaining({
        name: "Jane Doe",
        email: "jane@example.com",
        subject: "Question about a booking",
      })
    );
  });

  it("shows an error message when submission fails", async () => {
    const user = userEvent.setup();
    mockedContactService.submitContactForm.mockRejectedValue(new ApiError(500, "Something went wrong on our end."));
    renderPage();

    await fillValidForm(user);
    await user.click(screen.getByRole("button", { name: "Send Message" }));

    await waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent("Something went wrong on our end.");
    });
  });

  it("displays the fetched company phone and email as quick actions", async () => {
    renderPage();

    const callLink = await screen.findByRole("link", { name: "Call Us" });
    expect(callLink).toHaveAttribute("href", "tel:+41 79 000 00 00");
    expect(screen.getByRole("link", { name: "Email Us" })).toHaveAttribute("href", "mailto:info@example.com");
  });
});
