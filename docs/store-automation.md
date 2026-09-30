# Automated Microsoft Store releases

The **Microsoft Store release** GitHub Actions workflow is manually triggered.
It runs offline Python and native tests, builds an unsigned x64 MSIX, verifies
the package identity, then optionally uploads and commits a package-only Store
submission. Microsoft signs the Store package; no signing certificate is needed
in GitHub. Certification still takes place in Microsoft Store.

## One-time account setup

1. In Partner Center account settings, associate a Microsoft Entra directory
   with the developer account. Sign in with the directory's administrator
   account to manage users/applications. The personal Microsoft account used
   to publish manually is not itself a service credential.
   If no directory exists, follow Microsoft's
   [create a tenant in Partner Center](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/create-new-azure-ad-tenant)
   guide (**Tenants → Create Microsoft Entra ID**). Creation of the administrator
   account/password and any consent prompts must be completed by the owner.
2. Add an Entra application for this release workflow in Partner Center's
   **Users** management and assign it the **Manager** role required by the
   submission API. Copy the Tenant ID and Client ID. Create a client secret.
   This grants publishing access to the Partner Center account, so use a
   dedicated application and protect its secret.
3. In this GitHub repository, open **Settings → Environments → microsoft-store**.
   Configure these environment secrets directly in GitHub:
   - `STORE_TENANT_ID`
   - `STORE_CLIENT_ID`
   - `STORE_CLIENT_SECRET` (the secret value, not its identifier)
   Never put credentials in source files, chat, logs, or workflow inputs.
   Track the secret's expiry and rotate it before it expires.
4. Restrict the environment to `main`. If your account/plan supports environment
   reviewers, you can add a release reviewer. The manual **submit** trigger
   already represents the decision to publish after certification.
5. Run the workflow with action **preflight**. This checks authentication and
   access to Store app `9PPP5290T27G` without modifying any submission.

Microsoft's API prerequisites and role instructions:
[Create and manage submissions](https://learn.microsoft.com/en-us/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services).
An initial completed/published submission is required; this workflow is for
updates to the already published Photo Metadata Repair Inspector.

## Release an update

Open **Actions → Microsoft Store release → Run workflow**, using branch `main`.

- **build**: test and build only; no Store credentials or upload. Download the
  `store-package` artifact for testing/manual Partner Center upload.
- **submit**: test, build, upload, and commit. Provide a higher version such as
  `1.0.1.0` and public release notes. Once certification succeeds, the submission
  publishes automatically (`Immediate`). This is a production release action.
- **preflight**: check API credentials/app access without uploading.
- **status**: check an existing submission using its ID from the previous run.
  Run this again later while certification is pending. Status checks do not
  create submissions or alter them.

The version must have four components, a positive major component, values at
most 65535, and a final `.0`. It must exceed both the checked-in manifest version
and all published package versions. The workflow changes the manifest only in
its checkout; it does not commit to the repository. Choose a new version for
every release rather than retrying a version already published.

The workflow preserves inherited descriptions, screenshots, availability and
pricing. It updates release notes in the existing listings and replaces x64
packages; it stops if other architectures are present. The release workflow
does not verify the interactive UI or run the Windows App Certification Kit.
Complete those checks with a trusted test distribution before submitting.

Store-installed clients receive the published release through Microsoft Store
automatic updates, according to their device settings. An app updater is not
required. [Microsoft update guidance](https://support.microsoft.com/en-us/windows/apps/turn-on-automatic-app-updates).

## Recovery and certification checks

Only one release workflow may run at a time. The script refuses to overwrite
an existing pending submission and never deletes drafts or retries mutations
automatically. A network timeout can occur after a request succeeds; inspect
Partner Center and the saved submission ID before retrying.

The run summary and `store-submission-state` artifact contain the submission
ID and its most recently observed status. They never contain tokens, client
secrets, or the signed upload URL. `CommitStarted`, `PreProcessing`,
`Certification` and `Publishing` are pending states, not proof of publication.
Use **status** until it reaches `Published`. Failed statuses fail the workflow;
read Microsoft's detailed errors/certification report in Partner Center.

If upload fails after draft creation, inspect the draft in Partner Center.
This implementation does not resume a partly uploaded draft. Delete/abandon
that draft deliberately before starting a new release; never blindly rerun.
Do not mix edits in Partner Center with API edits to the same submission:
[Microsoft submission lifecycle](https://learn.microsoft.com/en-us/windows/uwp/monetize/manage-app-submissions).

GitHub must permit the authenticated client to push workflow files. A classic
OAuth/PAT credential needs the `workflow` scope as well as repository access.
If a push is rejected for this scope, refresh GitHub CLI access interactively
with `gh auth refresh -h github.com -s workflow` and retry the push. Do not
copy the resulting authentication token into the repository.
