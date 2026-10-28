import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import {
  archiveCrmEmailTemplate,
  createCrmEmailTemplate,
  getCrmEmailTemplates,
  unarchiveCrmEmailTemplate,
  updateCrmEmailTemplate,
} from "../../../../services/crmEmailTemplateService";
import AdminNav from "../../../../components/AdminNav";
import CrmNav from "../../../../components/CrmNav";
import PageHeader from "../../../../components/PageHeader";
import type { CrmEmailTemplateDto } from "../../../../types/crmEmailTemplate";
import EmailTemplateFormModal, { type CrmEmailTemplateFormValues } from "./EmailTemplateFormModal";

const PAGE_SIZE = 25;

function EmailTemplatesPage() {
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [templates, setTemplates] = useState<CrmEmailTemplateDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modalState, setModalState] = useState<{ template?: CrmEmailTemplateDto } | null>(null);

  const loadTemplates = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    setError(null);
    try {
      const result = await getCrmEmailTemplates({ search, page, pageSize: PAGE_SIZE }, accessToken);
      setTemplates(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load templates.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, search, page]);

  useEffect(() => {
    loadTemplates();
  }, [loadTemplates]);

  useEffect(() => {
    const timeout = setTimeout(() => {
      setPage(1);
      setSearch(searchInput);
    }, 300);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  const handleSave = async (values: CrmEmailTemplateFormValues) => {
    if (!accessToken) return;

    if (modalState?.template) {
      await updateCrmEmailTemplate(modalState.template.id, values, accessToken);
      showToast("Template updated.");
    } else {
      await createCrmEmailTemplate(values, accessToken);
      showToast("Template created.");
    }

    setModalState(null);
    await loadTemplates();
  };

  const handleToggleArchive = async (template: CrmEmailTemplateDto) => {
    if (!accessToken) return;
    try {
      if (template.isArchived) {
        await unarchiveCrmEmailTemplate(template.id, accessToken);
        showToast("Template restored.");
      } else {
        await archiveCrmEmailTemplate(template.id, accessToken);
        showToast("Template archived.");
      }
      await loadTemplates();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the template.");
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader
          title="CRM — Email Templates"
          description="Reusable, personalized drafts for the Inbox composer."
          actions={
            <button type="button" onClick={() => setModalState({})}>
              + New Template
            </button>
          }
        />
        <CrmNav />

        <div className="row" style={{ marginBottom: "1rem" }}>
          <input type="search" placeholder="Search templates..." value={searchInput} onChange={(e) => setSearchInput(e.target.value)} aria-label="Search templates" />
        </div>

        {error && <p role="alert">{error}</p>}

        {isLoading ? (
          <div className="stack">
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
          </div>
        ) : templates.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state__title">No Templates Found</p>
            <p>Create your first reusable email template.</p>
          </div>
        ) : (
          <div className="table-responsive" style={{ overflowX: "auto" }}>
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Category</th>
                  <th>Subject</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {templates.map((template) => (
                  <tr key={template.id}>
                    <td data-label="Name">{template.name}</td>
                    <td data-label="Category">{template.category}</td>
                    <td data-label="Subject">{template.subject}</td>
                    <td data-label="Actions">
                      <div className="row" style={{ gap: "var(--space-2)" }}>
                        <button type="button" className="btn-ghost" onClick={() => setModalState({ template })}>
                          Edit
                        </button>
                        <button type="button" className="btn-ghost" onClick={() => handleToggleArchive(template)}>
                          {template.isArchived ? "Restore" : "Archive"}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {totalPages > 1 && (
          <div className="row" style={{ marginTop: "1rem" }}>
            <button type="button" className="btn-secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Previous
            </button>
            <span>
              Page {page} of {totalPages}
            </span>
            <button type="button" className="btn-secondary" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
              Next
            </button>
          </div>
        )}

        {modalState && <EmailTemplateFormModal template={modalState.template} onSave={handleSave} onClose={() => setModalState(null)} />}
      </main>
    </div>
  );
}

export default EmailTemplatesPage;
