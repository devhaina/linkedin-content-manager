import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CampaignsService } from '../../../core/services/campaigns.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CampaignResponse } from '../../../core/models/campaign.model';
import { StatusBadge } from '../../../shared/status-badge/status-badge';

@Component({
  selector: 'app-campaign-list',
  imports: [RouterLink, StatusBadge],
  templateUrl: './campaign-list.html',
})
export class CampaignList implements OnInit {
  readonly campaigns = signal<CampaignResponse[]>([]);
  readonly loading = signal(true);

  constructor(
    private readonly campaignsService: CampaignsService,
    private readonly notifications: NotificationService,
    readonly auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.campaignsService
      .getAll({ pageSize: 100 })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => this.campaigns.set(result.items),
      });
  }

  archive(campaign: CampaignResponse): void {
    if (!confirm(`Archive "${campaign.name}"? It stops accepting new content but its history stays intact.`)) {
      return;
    }
    this.campaignsService.delete(campaign.id).subscribe({
      next: () => {
        this.notifications.success(`"${campaign.name}" archived.`);
        this.load();
      },
    });
  }
}
