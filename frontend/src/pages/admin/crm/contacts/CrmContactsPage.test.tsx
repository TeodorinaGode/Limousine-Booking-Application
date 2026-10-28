import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CrmContactsPage from "./CrmContactsPage";
import { ToastProvider } from "../../../../context/ToastContext";
import * as crmContactService from "../../../../services/crmContactService";
import * as administratorService from "../../../../services/administratorService";
import * as authContext from "../../../../context/AuthContext";
import type { CrmContactDto } from "../../../../types/crmContact";
import type { PagedResult } from "../../../../types/api";

vi.mock("../../../../services/crmContactService");
vi.mock("../../../../services/administratorService");
vi.mock("../../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedContactService = vi.mocked(crmContactService);
const mockedAdministratorService = vi.mocked(administratorService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeContact(overrides: Partial<CrmContactDto> = {}): CrmContactDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    firstName: "Maria",
    lastName: "Keller",
    email: "maria.keller@example.com",
    jobTitle: "Concierge Manager",
    phone: null,
    preferredContactMethod: null,
    status: "NewProspect",
    assignedOwner: null,
    lastContactedAt: null,
    nextFollowUpAt: null,
    notes: null,
    isArchived: false,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function pagedResult<T>(items: T[]): PagedResult<T> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <CrmContactsPage />
      </ToastProvider>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedUseAuth.mockReturnValue({
    user: { id: "u1", email: "admin@example.com", firstName: "Admin", lastName: "User", role: "Administrator", languageCode: null },
    accessToken: "test-token",
    expiresAt: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  });
  mockedAdministratorService.getAdministrators.mockResolvedValue([]);
});

describe("CrmContactsPage", () => {
  it("renders the contact list", async () => {
    mockedContactService.getCrmContacts.mockResolvedValue(
      pagedResult([makeContact(), makeContact({ id: "2", firstName: "Tom", lastName: "Fischer", email: "tom@example.com" })])
    );

    renderPage();

    expect(await screen.findByText("Maria Keller")).toBeInTheDocument();
    expect(screen.getByText("Tom Fischer")).toBeInTheDocument();
  });

  it("shows an error message when loading fails", async () => {
    mockedContactService.getCrmContacts.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("rejects a create-contact form with a duplicate email reported by the backend", async () => {
    mockedContactService.getCrmContacts.mockResolvedValue(pagedResult([]));
    mockedContactService.createCrmContact.mockRejectedValue(new Error("A contact with this email already exists."));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Contacts Found");

    await user.click(screen.getByRole("button", { name: "+ New Contact" }));
    await user.type(screen.getByLabelText("First name"), "Maria");
    await user.type(screen.getByLabelText("Last name"), "Keller");
    await user.type(screen.getByLabelText("Email"), "maria.keller@example.com");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("A contact with this email already exists.")).toBeInTheDocument();
  });

  it("creates a contact", async () => {
    mockedContactService.getCrmContacts.mockResolvedValue(pagedResult([]));
    mockedContactService.createCrmContact.mockResolvedValue(makeContact());
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Contacts Found");

    await user.click(screen.getByRole("button", { name: "+ New Contact" }));
    await user.type(screen.getByLabelText("First name"), "Maria");
    await user.type(screen.getByLabelText("Last name"), "Keller");
    await user.type(screen.getByLabelText("Email"), "maria.keller@example.com");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedContactService.createCrmContact).toHaveBeenCalledWith(
        expect.objectContaining({ firstName: "Maria", lastName: "Keller", email: "maria.keller@example.com" }),
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Contact created successfully.");
  });
});
