import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { submitContactForm } from "../../services/contactService";
import { ApiError } from "../../services/apiClient";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import { usePageMeta } from "../../hooks/usePageMeta";
import { useCompanyInfo } from "../../hooks/useCompanyInfo";

const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

interface ContactFormValues {
  name: string;
  email: string;
  phone: string;
  subject: string;
  message: string;
  preferredContactMethod: "" | "Phone" | "Email";
  preferredDate: string;
}

const initialValues: ContactFormValues = {
  name: "",
  email: "",
  phone: "",
  subject: "",
  message: "",
  preferredContactMethod: "",
  preferredDate: "",
};

/** Public Contact page + form (Prompt 17, section 17/18) — the customer needs no account; the form posts to POST /api/public/contact. */
function ContactPage() {
  const { t } = useTranslation(["site", "validation"]);
  usePageMeta(`${t("contact.title")} | ${t("footer.description")}`, t("contact.subtitle"));
  const company = useCompanyInfo();

  const [values, setValues] = useState<ContactFormValues>(initialValues);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitted, setIsSubmitted] = useState(false);

  const validate = (): Record<string, string> => {
    const nextErrors: Record<string, string> = {};
    if (!values.name.trim()) nextErrors.name = t("validation:required");
    if (!values.email.trim()) {
      nextErrors.email = t("validation:emailRequired");
    } else if (!EMAIL_PATTERN.test(values.email.trim())) {
      nextErrors.email = t("validation:emailInvalid");
    }
    if (!values.subject.trim()) nextErrors.subject = t("validation:required");
    if (!values.message.trim() || values.message.trim().length < 10) nextErrors.message = t("validation:required");
    return nextErrors;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(null);

    const validationErrors = validate();
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setIsSubmitting(true);
    try {
      await submitContactForm({
        name: values.name.trim(),
        email: values.email.trim(),
        phone: values.phone.trim() || undefined,
        subject: values.subject.trim(),
        message: values.message.trim(),
        preferredContactMethod: values.preferredContactMethod || undefined,
        preferredDate: values.preferredDate || undefined,
      });
      setIsSubmitted(true);
      setValues(initialValues);
    } catch (err) {
      setSubmitError(err instanceof ApiError ? err.message : err instanceof Error ? err.message : t("validation:required"));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div>
      <Header />

      <section className="section section--center">
        <div className="container container--medium">
          <p className="section__eyebrow">{t("nav.contact")}</p>
          <h1 className="section__title">{t("contact.title")}</h1>
          <p className="section__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>{t("contact.subtitle")}</p>
          {company && (
            <div className="row" style={{ justifyContent: "center", marginBottom: "var(--space-8)" }}>
              <a href={`tel:${company.phone}`} className="btn-secondary" style={{ display: "inline-block", padding: "0.6em 1.2em", borderRadius: "var(--radius-sm)" }}>
                {t("contact.callUs")}
              </a>
              <a href={`mailto:${company.email}`} className="btn-secondary" style={{ display: "inline-block", padding: "0.6em 1.2em", borderRadius: "var(--radius-sm)" }}>
                {t("contact.emailUs")}
              </a>
              <Link to="/booking" style={{ display: "inline-block", padding: "0.6em 1.2em" }}>
                <button type="button">{t("nav.bookARide")}</button>
              </Link>
            </div>
          )}
        </div>
      </section>

      <section className="section" style={{ paddingTop: 0 }}>
        <div className="container container--medium">
          <div className="card">
            <h2 style={{ marginTop: 0 }}>{t("contact.form.title")}</h2>

            {isSubmitted ? (
              <p role="status">{t("contact.form.success")}</p>
            ) : (
              <form onSubmit={handleSubmit} noValidate>
                <div className="row">
                  <div className="form-group" style={{ flex: 1 }}>
                    <label htmlFor="contact-name">{t("contact.form.name")}</label>
                    <br />
                    <input id="contact-name" type="text" value={values.name} onChange={(e) => setValues({ ...values, name: e.target.value })} />
                    {errors.name && <p className="form-error">{errors.name}</p>}
                  </div>
                  <div className="form-group" style={{ flex: 1 }}>
                    <label htmlFor="contact-email">{t("contact.form.email")}</label>
                    <br />
                    <input id="contact-email" type="email" value={values.email} onChange={(e) => setValues({ ...values, email: e.target.value })} />
                    {errors.email && <p className="form-error">{errors.email}</p>}
                  </div>
                </div>

                <div className="form-group">
                  <label htmlFor="contact-phone">{t("contact.form.phone")}</label>
                  <br />
                  <input id="contact-phone" type="tel" value={values.phone} onChange={(e) => setValues({ ...values, phone: e.target.value })} />
                </div>

                <div className="form-group">
                  <label htmlFor="contact-subject">{t("contact.form.subject")}</label>
                  <br />
                  <input id="contact-subject" type="text" value={values.subject} onChange={(e) => setValues({ ...values, subject: e.target.value })} />
                  {errors.subject && <p className="form-error">{errors.subject}</p>}
                </div>

                <div className="form-group">
                  <label htmlFor="contact-message">{t("contact.form.message")}</label>
                  <br />
                  <textarea id="contact-message" value={values.message} onChange={(e) => setValues({ ...values, message: e.target.value })} style={{ maxWidth: "100%" }} />
                  {errors.message && <p className="form-error">{errors.message}</p>}
                </div>

                <div className="row">
                  <div className="form-group" style={{ flex: 1 }}>
                    <label htmlFor="contact-preferred-method">{t("contact.form.preferredContactMethod")}</label>
                    <br />
                    <select
                      id="contact-preferred-method"
                      value={values.preferredContactMethod}
                      onChange={(e) => setValues({ ...values, preferredContactMethod: e.target.value as ContactFormValues["preferredContactMethod"] })}
                    >
                      <option value="">—</option>
                      <option value="Phone">{t("contact.form.phoneOption")}</option>
                      <option value="Email">{t("contact.form.emailOption")}</option>
                    </select>
                  </div>
                  <div className="form-group" style={{ flex: 1 }}>
                    <label htmlFor="contact-preferred-date">{t("contact.form.preferredDate")}</label>
                    <br />
                    <input
                      id="contact-preferred-date"
                      type="date"
                      value={values.preferredDate}
                      onChange={(e) => setValues({ ...values, preferredDate: e.target.value })}
                    />
                  </div>
                </div>

                {submitError && <p role="alert">{submitError}</p>}

                <button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? t("contact.form.sending") : t("contact.form.send")}
                </button>
              </form>
            )}
          </div>
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default ContactPage;
