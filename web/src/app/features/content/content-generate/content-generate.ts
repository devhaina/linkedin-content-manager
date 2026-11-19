import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CampaignsService } from '../../../core/services/campaigns.service';
import { ContentService } from '../../../core/services/content.service';
import { CampaignResponse } from '../../../core/models/campaign.model';
import { WRITER_PERSONA_LABELS, WriterPersona } from '../../../core/models/enums.model';

@Component({
  selector: 'app-content-generate',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './content-generate.html',
})
export class ContentGenerate implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly campaignsService = inject(CampaignsService);
  private readonly contentService = inject(ContentService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly campaigns = signal<CampaignResponse[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);

  readonly personaOptions = Object.values(WriterPersona);
  readonly personaLabels = WRITER_PERSONA_LABELS;

  readonly form = this.fb.nonNullable.group({
    campaignId: ['', [Validators.required]],
    topic: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(500)]],
    personaOverride: this.fb.control<WriterPersona | null>(null),
  });

  ngOnInit(): void {
    this.campaignsService
      .getAll({ pageSize: 100, status: undefined })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          const active = result.items.filter((c) => c.status === 'Active');
          this.campaigns.set(active);

          const preselect = this.route.snapshot.queryParamMap.get('campaignId');
          if (preselect && active.some((c) => c.id === preselect)) {
            this.form.patchValue({ campaignId: preselect });
          }
        },
      });
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const value = this.form.getRawValue();
    this.contentService
      .generate({ campaignId: value.campaignId, topic: value.topic, personaOverride: value.personaOverride })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (result) => this.router.navigate(['/content', result.executionId]),
      });
  }
}
