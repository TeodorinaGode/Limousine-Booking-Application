import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import { getCrmCompanies } from "../../../../services/crmCompanyService";
import { getCrmContacts } from "../../../../services/crmContactService";
import { getCrmDeals } from "../../../../services/crmDealService";
import {
  downloadCrmEmailAttachment,
  getCrmEmailThreadById,
  getCrmEmailThreads,
  linkCrmEmailThreadCompany,
  linkCrmEmailThreadContact,
  linkCrmEmailThreadDeal,
  markCrmEmailThreadRead,
  markCrmEmailThreadUnread,
  unlinkCrmEmailThreadCompany,
  unlinkCrmEmailThreadContact,
  unlinkCrmEmailThreadDeal,
} from "../../../../services/crmInboxService";
import AdminNav from "../../../../components/AdminNav";
import CrmNav from "../../../../components/CrmNav";
import PageHeader from "../../../../components/PageHeader";
import ComposeEmailModal, { type ComposeEmailPrefill } from "../../../../components/ComposeEmailModal";
import type { CrmCompanyDto } from "../../../../types/crmCompany";
import type { CrmContactDto } from "../../../../types/crmContact";
import type { CrmDealDto } from "../../../../types/crmDeal";
import type { CrmEmailMessageDto, CrmEmailThreadDetailDto, CrmEmailThreadListItemDto } from "../../../../types/crmEmailThread";

const PAGE_SIZE = 25;
const CANDIDATE_PAGE_SIZE = 200;

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString(undefined, { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" });
}

/** Strips (but remembers) remote image sources so opening a message never fires a tracking pixel without the user choosing "Load images" first. */
function prepareHtmlForDisplay(html: string, allowImages: boolean): string {
  if (allowImages) return html;
  const doc = new DOMParser().parseFromString(html, "text/html");
  doc.querySelectorAll("img[src]").forEach((img) => {
    const src = img.getAttribute("src") ?? "";
    img.setAttribute("data-blocked-src", src);
    img.removeAttribute("src");
  });
  return doc.body.innerHTML;
}

function EmailBodyFrame({ html }: { html: string }) {
  const [allowImages, setAllowImages] = useState(false);
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const hasRemoteImages = useMemo(() => /<img[^>]+src=/i.test(html), [html]);

  const srcDoc = useMemo(() => {
    const body = prepareHtmlForDisplay(html, allowImages);
    return `<!doctype html><html><head><meta charset="utf-8"><style>
      body { font-family: 'Inter', sans-serif; color: #f5f5f5; background: #111111; margin: 0; padding: 12px; word-wrap: break-word; }
      img { max-width: 100%; }
      a { color: #d8d8d8; }
    </style></head><body>${body}</body></html>`;
  }, [html, allowImages]);

  const handleLoad = () => {
    const doc = iframeRef.current?.contentDocument;
    if (doc?.body) iframeRef.current!.style.height = `${doc.body.scrollHeight + 24}px`;
  };

  return (
    <div>
      {hasRemoteImages && !allowImages && (
        <button type="button" className="btn-ghost" onClick={() => setAllowImages(true)} style={{ marginBottom: "var(--space-2)" }}>
          Load images
        </button>
      )}
      <iframe
        ref={iframeRef}
        className="email-message__frame"
        sandbox="allow-same-origin"
        srcDoc={srcDoc}
        onLoad={handleLoad}
        title="Email message body"
      />
    </div>
  );
}

function formatAddressList(addresses: { address: string; name: string | null }[]): string {
  return addresses.map((a) => (a.name ? `${a.name} <${a.address}>` : a.address)).join(", ");
}

function EmailMessageCard({ message, accessToken }: { message: CrmEmailMessageDto; accessToken: string }) {
  return (
    <article className={`email-message${message.direction === "Outbound" ? " email-message--outbound" : ""}`}>
      <div className="email-message__header">
        <div>
          <strong>{message.fromName ?? message.fromAddress}</strong> &lt;{message.fromAddress}&gt;
          <div>To: {formatAddressList(message.toAddresses) || "—"}</div>
          {message.ccAddresses.length > 0 && <div>Cc: {formatAddressList(message.ccAddresses)}</div>}
        </div>
        <span>{formatDateTime(message.occurredAt)}</span>
      </div>

      {message.bodyHtml ? (
        <EmailBodyFrame html={message.bodyHtml} />
      ) : (
        <div style={{ whiteSpace: "pre-wrap" }}>{message.bodyPlainText}</div>
      )}

      {message.attachments.length > 0 && (
        <div className="email-message__attachments">
          {message.attachments.map((attachment) => (
            <button
              key={attachment.id}
              type="button"
              className="badge"
              onClick={() => downloadCrmEmailAttachment(message.id, attachment.id, attachment.fileName, accessToken)}
            >
              📎 {attachment.fileName} ({Math.ceil(attachment.sizeBytes / 1024)} KB)
            </button>
          ))}
        </div>
      )}
    </article>
  );
}

function CrmInboxPage() {
  const { accessToken } = useAuth();
  const { showToast } = useToast();
  const [searchParams, setSearchParams] = useSearchParams();

  const [threads, setThreads] = useState<CrmEmailThreadListItemDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [isLoadingList, setIsLoadingList] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [selectedThreadId, setSelectedThreadId] = useState<string | null>(null);
  const [detail, setDetail] = useState<CrmEmailThreadDetailDto | null>(null);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);

  const [companies, setCompanies] = useState<CrmCompanyDto[]>([]);
  const [contacts, setContacts] = useState<CrmContactDto[]>([]);
  const [deals, setDeals] = useState<CrmDealDto[]>([]);
  const [linkCompanyId, setLinkCompanyId] = useState("");
  const [linkContactId, setLinkContactId] = useState("");
  const [linkDealId, setLinkDealId] = useState("");
  const [isLinking, setIsLinking] = useState(false);

  const [composePrefill, setComposePrefill] = useState<ComposeEmailPrefill | null>(null);

  const loadThreads = useCallback(async () => {
    if (!accessToken) return;
    setIsLoadingList(true);
    setError(null);
    try {
      const result = await getCrmEmailThreads({ search, unreadOnly: unreadOnly || undefined, page, pageSize: PAGE_SIZE }, accessToken);
      setThreads(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the inbox.");
    } finally {
      setIsLoadingList(false);
    }
  }, [accessToken, search, unreadOnly, page]);

  useEffect(() => {
    loadThreads();
  }, [loadThreads]);

  useEffect(() => {
    const timeout = setTimeout(() => {
      setPage(1);
      setSearch(searchInput);
    }, 300);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      try {
        const [companyResult, contactResult, dealResult] = await Promise.all([
          getCrmCompanies({ pageSize: CANDIDATE_PAGE_SIZE }, accessToken),
          getCrmContacts({ pageSize: CANDIDATE_PAGE_SIZE }, accessToken),
          getCrmDeals({ pageSize: CANDIDATE_PAGE_SIZE }, accessToken),
        ]);
        setCompanies(companyResult.items);
        setContacts(contactResult.items);
        setDeals(dealResult.items);
      } catch {
        // Non-fatal — the link dropdowns just stay empty.
      }
    })();
  }, [accessToken]);

  const loadDetail = useCallback(
    async (threadId: string, markAsReadIfUnread: boolean) => {
      if (!accessToken) return;
      setIsLoadingDetail(true);
      try {
        let result = await getCrmEmailThreadById(threadId, accessToken);
        if (markAsReadIfUnread && result.hasUnread) {
          result = await markCrmEmailThreadRead(threadId, accessToken);
          setThreads((prev) => prev.map((t) => (t.id === threadId ? { ...t, hasUnread: false } : t)));
        }
        setDetail(result);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load the conversation.");
      } finally {
        setIsLoadingDetail(false);
      }
    },
    [accessToken]
  );

  const handleSelectThread = (threadId: string) => {
    setSelectedThreadId(threadId);
    loadDetail(threadId, true);
  };

  // A "View conversation" link from an activity timeline (Company/Contact/Deal
  // detail page, or the global Activities/Overview pages) arrives here as
  // ?thread=<id> — open it once, then drop the query param so a page refresh
  // doesn't keep re-selecting it over whatever the user navigates to next.
  useEffect(() => {
    const threadId = searchParams.get("thread");
    if (!threadId) return;

    handleSelectThread(threadId);
    setSearchParams({}, { replace: true });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleToggleRead = async () => {
    if (!accessToken || !selectedThreadId || !detail) return;
    try {
      const updated = detail.hasUnread
        ? await markCrmEmailThreadRead(selectedThreadId, accessToken)
        : await markCrmEmailThreadUnread(selectedThreadId, accessToken);
      setDetail(updated);
      setThreads((prev) => prev.map((t) => (t.id === selectedThreadId ? { ...t, hasUnread: updated.hasUnread } : t)));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the conversation.");
    }
  };

  const handleLinkCompany = async () => {
    if (!accessToken || !selectedThreadId || !linkCompanyId) return;
    setIsLinking(true);
    try {
      setDetail(await linkCrmEmailThreadCompany(selectedThreadId, linkCompanyId, accessToken));
      setLinkCompanyId("");
      showToast("Company linked.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to link the company.");
    } finally {
      setIsLinking(false);
    }
  };

  const handleUnlinkCompany = async (companyId: string) => {
    if (!accessToken || !selectedThreadId) return;
    try {
      setDetail(await unlinkCrmEmailThreadCompany(selectedThreadId, companyId, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to unlink the company.");
    }
  };

  const handleLinkContact = async () => {
    if (!accessToken || !selectedThreadId || !linkContactId) return;
    setIsLinking(true);
    try {
      setDetail(await linkCrmEmailThreadContact(selectedThreadId, linkContactId, accessToken));
      setLinkContactId("");
      showToast("Contact linked.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to link the contact.");
    } finally {
      setIsLinking(false);
    }
  };

  const handleUnlinkContact = async (contactId: string) => {
    if (!accessToken || !selectedThreadId) return;
    try {
      setDetail(await unlinkCrmEmailThreadContact(selectedThreadId, contactId, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to unlink the contact.");
    }
  };

  const handleLinkDeal = async () => {
    if (!accessToken || !selectedThreadId || !linkDealId) return;
    setIsLinking(true);
    try {
      setDetail(await linkCrmEmailThreadDeal(selectedThreadId, linkDealId, accessToken));
      setLinkDealId("");
      showToast("Deal linked.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to link the deal.");
    } finally {
      setIsLinking(false);
    }
  };

  const handleUnlinkDeal = async (dealId: string) => {
    if (!accessToken || !selectedThreadId) return;
    try {
      setDetail(await unlinkCrmEmailThreadDeal(selectedThreadId, dealId, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to unlink the deal.");
    }
  };

  const handleOpenReply = () => {
    if (!detail) return;
    const latestInbound = [...detail.messages].reverse().find((m) => m.direction === "Inbound");
    setComposePrefill({
      threadId: detail.id,
      toAddresses: latestInbound ? [{ address: latestInbound.fromAddress, name: latestInbound.fromName }] : [],
      subject: detail.subject.toLowerCase().startsWith("re:") ? detail.subject : `Re: ${detail.subject}`,
      companyId: detail.companies[0]?.id ?? null,
      contactId: detail.contacts[0]?.id ?? null,
      dealId: detail.deals[0]?.id ?? null,
    });
  };

  const handleComposeSent = async () => {
    setComposePrefill(null);
    showToast("Email sent.");
    await loadThreads();
    if (selectedThreadId) await loadDetail(selectedThreadId, false);
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const availableCompanies = companies.filter((c) => !detail?.companies.some((linked) => linked.id === c.id));
  const availableContacts = contacts.filter((c) => !detail?.contacts.some((linked) => linked.id === c.id));
  const availableDeals = deals.filter((d) => !detail?.deals.some((linked) => linked.id === d.id));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader
          title="CRM — Inbox"
          description="Real synced email, organized by conversation and linked to your CRM records."
          actions={
            <div className="row">
              <button type="button" onClick={() => setComposePrefill({})}>
                + New Email
              </button>
              <Link to="/admin/crm/inbox/templates" className="btn-secondary">
                Templates
              </Link>
              <Link to="/admin/crm/inbox/settings" className="btn-secondary">
                Email Settings
              </Link>
            </div>
          }
        />
        <CrmNav />

        {error && <p role="alert">{error}</p>}

        <div className="row" style={{ marginBottom: "1rem" }}>
          <input
            type="search"
            placeholder="Search conversations..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            aria-label="Search conversations"
          />
          <label>
            <input
              type="checkbox"
              checked={unreadOnly}
              onChange={(e) => {
                setPage(1);
                setUnreadOnly(e.target.checked);
              }}
            />{" "}
            Unread only
          </label>
        </div>

        <div className="email-inbox" data-pane={selectedThreadId ? "detail" : "list"}>
          <div className="email-thread-list">
            {isLoadingList ? (
              <div className="stack">
                <div className="skeleton skeleton-line" style={{ height: 60 }} />
                <div className="skeleton skeleton-line" style={{ height: 60 }} />
              </div>
            ) : threads.length === 0 ? (
              <div className="empty-state">
                <p className="empty-state__title">No Conversations</p>
                <p>Connect a mailbox in Email Settings to start syncing real messages.</p>
              </div>
            ) : (
              threads.map((thread) => (
                <button
                  key={thread.id}
                  type="button"
                  className={`email-thread-list__item${thread.hasUnread ? " email-thread-list__item--unread" : ""}${thread.id === selectedThreadId ? " email-thread-list__item--active" : ""}`}
                  onClick={() => handleSelectThread(thread.id)}
                >
                  <div className="email-thread-list__row">
                    <span>{thread.latestFromName ?? thread.latestFromAddress ?? "Unknown sender"}</span>
                    <span>{formatDateTime(thread.lastMessageAt)}</span>
                  </div>
                  <p className="email-thread-list__subject">{thread.subject}</p>
                  {(thread.associatedCompanyNames.length > 0 || thread.associatedContactNames.length > 0 || thread.associatedDealNames.length > 0) && (
                    <div className="email-thread-list__tags">
                      {[...thread.associatedCompanyNames, ...thread.associatedContactNames, ...thread.associatedDealNames].map((name) => (
                        <span key={name} className="badge">
                          {name}
                        </span>
                      ))}
                    </div>
                  )}
                </button>
              ))
            )}

            {totalPages > 1 && (
              <div className="row" style={{ marginTop: "var(--space-2)" }}>
                <button type="button" className="btn-secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                  Previous
                </button>
                <span>
                  {page} / {totalPages}
                </span>
                <button type="button" className="btn-secondary" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                  Next
                </button>
              </div>
            )}
          </div>

          <div className="email-conversation">
            {selectedThreadId && (
              <button type="button" className="btn-ghost" onClick={() => setSelectedThreadId(null)} style={{ marginBottom: "var(--space-2)" }}>
                ← Back to Inbox
              </button>
            )}

            {!selectedThreadId ? (
              <p>Select a conversation to read it.</p>
            ) : isLoadingDetail || !detail ? (
              <div className="skeleton skeleton-line" style={{ height: 200 }} />
            ) : (
              <>
                <div className="row" style={{ justifyContent: "space-between", marginBottom: "var(--space-3)" }}>
                  <h2 style={{ margin: 0 }}>{detail.subject}</h2>
                  <div className="row">
                    <button type="button" className="btn-secondary" onClick={handleOpenReply}>
                      Reply
                    </button>
                    <button type="button" className="btn-ghost" onClick={handleToggleRead}>
                      Mark as {detail.hasUnread ? "Read" : "Unread"}
                    </button>
                  </div>
                </div>

                {accessToken && detail.messages.map((message) => <EmailMessageCard key={message.id} message={message} accessToken={accessToken} />)}

                <section className="email-association-group">
                  <h3>Companies</h3>
                  {detail.companies.length === 0 ? <p>No company linked yet.</p> : (
                    <ul>
                      {detail.companies.map((company) => (
                        <li key={company.id}>
                          {company.name}{" "}
                          <button type="button" className="btn-ghost" onClick={() => handleUnlinkCompany(company.id)}>
                            Unlink
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                  <div className="row">
                    <select value={linkCompanyId} onChange={(e) => setLinkCompanyId(e.target.value)} aria-label="Company to link">
                      <option value="">Select a company to link...</option>
                      {availableCompanies.map((company) => (
                        <option key={company.id} value={company.id}>
                          {company.name}
                        </option>
                      ))}
                    </select>
                    <button type="button" className="btn-secondary" disabled={!linkCompanyId || isLinking} onClick={handleLinkCompany}>
                      Link Company
                    </button>
                  </div>
                </section>

                <section className="email-association-group">
                  <h3>Contacts</h3>
                  {detail.contacts.length === 0 ? <p>No contact linked yet.</p> : (
                    <ul>
                      {detail.contacts.map((contact) => (
                        <li key={contact.id}>
                          {contact.firstName} {contact.lastName} ({contact.email}){" "}
                          <button type="button" className="btn-ghost" onClick={() => handleUnlinkContact(contact.id)}>
                            Unlink
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                  <div className="row">
                    <select value={linkContactId} onChange={(e) => setLinkContactId(e.target.value)} aria-label="Contact to link">
                      <option value="">Select a contact to link...</option>
                      {availableContacts.map((contact) => (
                        <option key={contact.id} value={contact.id}>
                          {contact.firstName} {contact.lastName} ({contact.email})
                        </option>
                      ))}
                    </select>
                    <button type="button" className="btn-secondary" disabled={!linkContactId || isLinking} onClick={handleLinkContact}>
                      Link Contact
                    </button>
                  </div>
                </section>

                <section className="email-association-group">
                  <h3>Deals</h3>
                  {detail.deals.length === 0 ? <p>No deal linked yet.</p> : (
                    <ul>
                      {detail.deals.map((deal) => (
                        <li key={deal.id}>
                          {deal.name} ({deal.stageName}){" "}
                          <button type="button" className="btn-ghost" onClick={() => handleUnlinkDeal(deal.id)}>
                            Unlink
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                  <div className="row">
                    <select value={linkDealId} onChange={(e) => setLinkDealId(e.target.value)} aria-label="Deal to link">
                      <option value="">Select a deal to link...</option>
                      {availableDeals.map((deal) => (
                        <option key={deal.id} value={deal.id}>
                          {deal.name}
                        </option>
                      ))}
                    </select>
                    <button type="button" className="btn-secondary" disabled={!linkDealId || isLinking} onClick={handleLinkDeal}>
                      Link Deal
                    </button>
                  </div>
                </section>
              </>
            )}
          </div>
        </div>

        {composePrefill && (
          <ComposeEmailModal prefill={composePrefill} onClose={() => setComposePrefill(null)} onSent={handleComposeSent} />
        )}
      </main>
    </div>
  );
}

export default CrmInboxPage;
