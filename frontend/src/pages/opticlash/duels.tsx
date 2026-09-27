import { ClashTabs } from "@/components/opticlash/clash-tabs";
import { CreateDuelModal } from "@/components/opticlash/create-duel-modal";
import { DuelCard } from "@/components/opticlash/duel-card";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { PageTitle } from "@/components/ui/page-title";
import { SearchInput } from "@/components/ui/search-input";
import { customFetch } from "@/lib/custom-fetch";
import type { DuelSummary, UserDuelsResponse } from "@/types/clash";
import { ArrowLeft, Plus, Trophy, Swords, Flame, CheckCircle2 } from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";

interface KpiStats {
    won: number;
    lost: number;
    active: number;
    winRate: number;
}

export default function AllDuelsPage() {
    const [filterStatus, setFilterStatus] = useState<'all' | 'active' | 'finished'>('all');
    const [isCreateDuelOpen, setIsCreateDuelOpen] = useState(false);

    const [duelsList, setDuelsList] = useState<DuelSummary[]>([]);
    const [kpiStats, setKpiStats] = useState<KpiStats>({ won: 0, lost: 0, active: 0, winRate: 0 });
    const [searchQuery, setSearchQuery] = useState('');
    const [isLoading, setIsLoading] = useState(true);

    const navigate = useNavigate();

    const fetchDuels = async () => {
        try {
            const res = await customFetch('/api/clash/duels');
            if (res.ok) {
                const data: UserDuelsResponse = await res.json();
                setDuelsList(data.duels || []);
                setKpiStats({
                    won: data.won || 0,
                    lost: data.lost || 0,
                    active: data.active || 0,
                    winRate: data.winRatePercent || 0,
                });
            }
        } catch {
            //fallback gracefully
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect -- fetch-on-mount
        void fetchDuels();
    }, []);

    const filterbyStatus = (duel: DuelSummary): boolean => {
        const statuslow = duel.status?.toLowerCase();
        if (filterStatus === 'active') {
            return statuslow === 'active';
        }
        if (filterStatus === 'finished') {
            return statuslow === 'finished';
        }
        return true;
    };

    const filterbyQuery = (duel: DuelSummary): boolean => {
        if (!searchQuery.trim()) {
            return true;
        }
        const q = searchQuery.toLowerCase();
        const rivalMatch = duel.rivalName?.toLowerCase().includes(q) ?? false;
        const userMatch = duel.challengerName?.toLowerCase().includes(q) ?? false;
        const exerMatch = duel.exerciseName?.toLowerCase().includes(q) ?? false;
        const titleMatch = duel.title?.toLowerCase().includes(q) ?? false;

        return rivalMatch || userMatch || exerMatch || titleMatch;
    };

    const sortDuels = (a: DuelSummary, b: DuelSummary): number => {
        const aActive = a.status?.toLowerCase() === 'active';
        const bActive = b.status?.toLowerCase() === 'active';

        if (aActive && bActive && a.endDate && b.endDate) {
            return new Date(a.endDate).getTime() - new Date(b.endDate).getTime();
        }
        const timeA = a.startDate ? new Date(a.startDate).getTime() : 0;
        const timeB = b.startDate ? new Date(b.startDate).getTime() : 0;
        return timeB - timeA;
    };

    const filteredDuels = duelsList.filter(filterbyStatus).filter(filterbyQuery).sort(sortDuels);

    const renderDuels = () => {
        if (isLoading && duelsList.length === 0) {
            return (
                <Card className="bg-surface border-border p-10 text-center text-muted-foreground font-sans text-xs">
                    Loading duels...
                </Card>
            );
        }
        if (filteredDuels.length === 0) {
            return (
                <Card className="bg-surface border-border p-10 text-center text-muted-foreground font-sans text-xs">
                    No duels found matching your criteria.
                </Card>
            );
        }

        return (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {filteredDuels.map((duel) => (
                    <DuelCard key={duel.id} duel={duel} onClick={() => navigate(`/clash/duels/${duel.id}`)}/>
                ))}
            </div>
        );
    };

    return (
        <div className="min-h-screen bg-background text-foreground pb-20">
            {/* header */}
            <div className="max-w-6xl mx-auto px-4 pt-8 pb-4">
                <Link to="/clash" className="inline-flex items-center gap-2 text-xs font-bold text-muted-foreground hover:text-brand uppercase tracking-[1px] mb-4 transition font-sans">
                    <ArrowLeft className="w-4 h-4"/> Back to Arena Hub
                </Link>

                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div>
                        <PageTitle title="1V1 DUELS"/>
                    </div>

                    <Button variant="default" size="sm" onClick={() => setIsCreateDuelOpen(true)} className="h-9 text-xs flex items-center gap-1.5 self-start md:self-auto">
                        <Plus className="w-3.5 h-3.5"/>
                        <span>Challenge a Friend</span>
                    </Button>
                </div>
            </div>

            <div className="max-w-6xl mx-auto px-4 py-4 space-y-6">
                {/* kpi stats */}
                <div className="grid grid-cols-2 md:grid-cols-4 gap-3.5">
                    <Card className="bg-surface border-border p-4 shadow-sm">
                        <CardContent className="p-0 flex items-center justify-between">
                            <div>
                                <span className="text-[11px] uppercase font-bold text-muted-foreground block font-sans tracking-[1px]">
                                    Duels Won
                                </span>
                                <span className="font-display text-2xl font-bold text-success mt-0.5 block">
                                    {kpiStats.won}
                                </span>
                            </div>
                            <div className="w-10 h-10 rounded-xl bg-success/10 border border-success/30 flex items-center justify-center text-success">
                                <Trophy className="w-5 h-5"/>
                            </div>
                        </CardContent>
                    </Card>
                    <Card className="bg-surface border-border p-4 shadow-sm">
                        <CardContent className="p-0 flex items-center justify-between">
                            <div>
                                <span className="text-[11px] uppercase font-bold text-muted-foreground block font-sans tracking-[1px]">
                                    Duels Lost
                                </span>
                                <span className="font-display text-2xl font-bold text-muted-foreground mt-0.5 block">
                                    {kpiStats.lost}
                                </span>
                            </div>
                            <div className="w-10 h-10 rounded-xl bg-surface-2 border border-border flex items-center justify-center text-muted-foreground">
                                <Swords className="w-5 h-5"/>
                            </div>
                        </CardContent>
                    </Card>

                    <Card className="bg-surface border-border p-4 shadow-sm">
                        <CardContent className="p-0 flex items-center justify-between">
                            <div>
                                <span className="text-[11px] uppercase font-bold text-muted-foreground block font-sans tracking-[1px]">
                                    Active Duels
                                </span>
                                <span className="font-display text-2xl font-bold text-brand mt-0.5 block">
                                    {kpiStats.active}
                                </span>
                            </div>
                            <div className="w-10 h-10 rounded-xl bg-brand-fill border border-brand/30 flex items-center justify-center text-brand">
                                <Flame className="w-5 h-5"/>
                            </div>
                        </CardContent>
                    </Card>

                    <Card className="bg-surface border-border p-4 shadow-sm">
                        <CardContent className="p-0 flex items-center justify-between">
                            <div>
                                <span className="text-[11px] uppercase font-bold text-muted-foreground block font-sans tracking-[1px]">
                                    Win Rate
                                </span>
                                <span className="font-display text-2xl font-bold text-foreground mt-0.5 block">
                                    {kpiStats.winRate}%
                                </span>
                            </div>
                            <div className="w-10 h-10 rounded-xl bg-surface-2 border border-border flex items-center justify-center text-muted-foreground">
                                <CheckCircle2 className="w-5 h-5"/>
                            </div>
                        </CardContent>
                    </Card>
                </div>

                {/* filter+ search bar */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                    <div className="w-full max-w-md">
                        <SearchInput placeholder="Search by friend name, lift, or exercise" value={searchQuery} onChange={(e) => setSearchQuery(e.target.value)} className="h-9 text-xs bg-surface border-border font-sans"/>
                    </div>

                    <div className="w-full sm:w-auto">
                        <ClashTabs activeTab={filterStatus}
                            onChange={(status) => {
                                if (status === 'all' || status === 'active' || status === 'finished') {
                                    setFilterStatus(status);
                                }
                            }}
                            tabs={[
                                { id: 'all', label: 'All Duels' },
                                { id: 'active', label: 'Active' },
                                { id: 'finished', label: 'Finished' },
                            ]}/>
                    </div>
                </div>
                {/* duels list grid */}
                {renderDuels()}
            </div>

            <CreateDuelModal isOpen={isCreateDuelOpen} onClose={() => setIsCreateDuelOpen(false)} onDuelCreated={() => void fetchDuels()}/>
        </div>
    );
}