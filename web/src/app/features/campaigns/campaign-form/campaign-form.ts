import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CampaignsService } from '../../../core/services/campaigns.service';
import { NichesService } from '../../../core/services/niches.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Niche } from '../../../core/models/niche.model';
import { CampaignStatus, WRITER_PERSONA_LABELS, WriterPersona } from '../../../core/models/enums.model';

@Component({
  selector: 'app-campaign-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './campaign-form.html',
})
export class CampaignForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly campaignsService = inject(CampaignsService);
  private readonly nichesService = inject(NichesService);
  private readonly notifications = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly niches = signal<Niche[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly isEditMode = signal(false);
  readonly campaignId = signal<string | null>(null);
  readonly nicheName = signal<string | null>(null);

  readonly personaOptions = Object.values(WriterPersona);
  readonly personaLabels = WRITER_PERSONA_LABELS;
  readonly statusOptions = Object.values(CampaignStatus);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    nicheId: ['', [Validators.required]],
    defaultPersona: this.fb.nonNullable.control<WriterPersona>(WriterPersona.Practitioner, [Validators.required]),
    status: this.fb.nonNullable.control<CampaignStatus>(CampaignStatus.Active),
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.isEditMode.set(!!id);
    this.campaignId.set(id);

    this.nichesService.getAll().subscribe({ next: (niches) => this.niches.set(niches) });

    if (id) {
      this.campaignsService.getById(id).subscribe({
        next: (campaign) => {
          this.nicheName.set(campaign.nicheName);
          this.form.patchValue({
            name: campaign.name,
            nicheId: campaign.nicheId,
            defaultPersona: campaign.defaultPersona,
            status: campaign.status,
          });
          this.form.controls.nicheId.disable();
          this.loading.set(false);
        },
      });
    } else {
      this.loading.set(false);
    }
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const value = this.form.getRawValue();
    const id = this.campaignId();

    const request$ = id
      ? this.campaignsService.update(id, { name: value.name, defaultPersona: value.defaultPersona, status: value.status })
      : this.campaignsService.create({ name: value.name, nicheId: value.nicheId, defaultPersona: value.defaultPersona });

    request$.pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: () => {
        this.notifications.success(id ? 'Campaign updated.' : 'Campaign created.');
        this.router.navigate(['/campaigns']);
      },
    });
  }
}
