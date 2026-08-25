import { useEffect, useState } from "react";
import { getCompanyInfo } from "../../services/companyService";
import AdminNav from "../../components/AdminNav";
import PageHeader from "../../components/PageHeader";
import type { CompanyInfoDto } from "../../types/company";

function displayValue(value: string | null | undefined): string {
  return value && value.trim() ? value : "—";
}

/**
 * Read-only company/operating settings — CompanySettings (backend) currently
 * has no PUT endpoint, only the public GET this page reuses (`useCompanyInfo`
 * swallows errors for its marketing-page use case, so this page calls
 * `getCompanyInfo` directly instead, to get a real loading/error state).
 * Deliberately not styled as an editable form — there's nothing here that
 * would actually save, and a form that silently does nothing on submit would
 * be worse than no form at all.
 */
function SettingsPage() {
  const [company, setCompany] = useState<CompanyInfoDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        setCompany(await getCompanyInfo());
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load company settings.");
      } finally {
        setIsLoading(false);
      }
    })();
  }, []);

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main app-main--narrow">
        <PageHeader
          title="Settings"
          description="Company and operating information shown across the public site."
        />

        <p className="text-muted" style={{ marginBottom: "var(--space-6)" }}>
          These details are configured on the server and shown here for reference. Editing support is not available yet.
        </p>

        {error && <p role="alert">{error}</p>}

        {isLoading ? (
          <div className="stack">
            <div className="skeleton skeleton-line" style={{ height: 140 }} />
            <div className="skeleton skeleton-line" style={{ height: 140 }} />
            <div className="skeleton skeleton-line" style={{ height: 100 }} />
          </div>
        ) : (
          company && (
            <div className="stack" style={{ gap: "var(--space-6)" }}>
              <section className="card">
                <h2 style={{ marginTop: 0 }}>General</h2>
                <dl>
                  <dt>Company Name</dt>
                  <dd>{displayValue(company.companyName)}</dd>

                  <dt>Tagline</dt>
                  <dd>{displayValue(company.tagline)}</dd>

                  <dt>Description</dt>
                  <dd>{displayValue(company.description)}</dd>
                </dl>
              </section>

              <section className="card">
                <h2 style={{ marginTop: 0 }}>Contact</h2>
                <dl>
                  <dt>Phone</dt>
                  <dd>{displayValue(company.phone)}</dd>

                  <dt>Emergency Phone</dt>
                  <dd>{displayValue(company.emergencyPhone)}</dd>

                  <dt>Email</dt>
                  <dd>{displayValue(company.email)}</dd>

                  <dt>Address</dt>
                  <dd>{displayValue(company.address)}</dd>

                  <dt>Website</dt>
                  <dd>{displayValue(company.website)}</dd>

                  <dt>Opening Hours</dt>
                  <dd>{displayValue(company.openingHours)}</dd>
                </dl>
              </section>

              <section className="card">
                <h2 style={{ marginTop: 0 }}>Operating Area</h2>
                {company.operatingCountryCodes.length === 0 ? (
                  <p className="text-muted">No operating countries configured.</p>
                ) : (
                  <div className="row">
                    {company.operatingCountryCodes.map((code) => (
                      <span key={code} className="badge badge--confirmed">{code}</span>
                    ))}
                  </div>
                )}
              </section>

              <section className="card">
                <h2 style={{ marginTop: 0 }}>Social Media</h2>
                {!company.facebookUrl && !company.instagramUrl && !company.whatsAppUrl ? (
                  <p className="text-muted">No social media links configured.</p>
                ) : (
                  <dl>
                    {company.facebookUrl && (
                      <>
                        <dt>Facebook</dt>
                        <dd><a href={company.facebookUrl} target="_blank" rel="noopener noreferrer">{company.facebookUrl}</a></dd>
                      </>
                    )}
                    {company.instagramUrl && (
                      <>
                        <dt>Instagram</dt>
                        <dd><a href={company.instagramUrl} target="_blank" rel="noopener noreferrer">{company.instagramUrl}</a></dd>
                      </>
                    )}
                    {company.whatsAppUrl && (
                      <>
                        <dt>WhatsApp</dt>
                        <dd><a href={company.whatsAppUrl} target="_blank" rel="noopener noreferrer">{company.whatsAppUrl}</a></dd>
                      </>
                    )}
                  </dl>
                )}
              </section>
            </div>
          )
        )}
      </main>
    </div>
  );
}

export default SettingsPage;
