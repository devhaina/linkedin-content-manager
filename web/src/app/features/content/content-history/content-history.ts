import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ContentService } from '../../../core/services/content.service';
import { ContentHistoryItemResponse } from '../../../core/models/content.model';
import { StatusBadge } from '../../../shared/status-badge/status-badge';

@Component({
  selector: 'app-content-history',
  imports: [RouterLink, StatusBadge, DatePipe],
  templateUrl: './content-history.html',
})
export class ContentHistory implements OnInit {
  readonly items = signal<ContentHistoryItemResponse[]>([]);
  readonly loading = signal(true);

  constructor(private readonly contentService: ContentService) {}

  ngOnInit(): void {
    this.contentService
      .getHistory({ pageSize: 100 })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({ next: (result) => this.items.set(result.items) });
  }
}
