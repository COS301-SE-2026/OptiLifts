import { AddFriendModal } from "@/components/opticlash/add-friend-modal";
import { AthleteAvatar } from "@/components/opticlash/athlete-avatar";
import { ClashTabs } from "@/components/opticlash/clash-tabs";
import { TierBadge } from "@/components/opticlash/tier-badge";
import { toast } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { PageTitle } from "@/components/ui/page-title";
import { SearchInput } from "@/components/ui/search-input";
import { customFetch } from "@/lib/custom-fetch";
import { ArrowLeft, Check, CheckCircle2, Copy, Trash2, UserPlus, Users, XCircle } from "lucide-react";
import { useState, useEffect } from "react";
import { Link } from "react-router-dom";

export default function FriendsManagementPage(){
    const [activeTab, setActiveTab] = useState<'friends' | 'requests'>('friends');
    const [searchQuery, setSearchQuery] = useState('');
    const [requestSearchQuery, setRequestSearchQuery] = useState('');
    const [copiedCode, setCopiedCode] = useState(false);

    interface FriendItem{
        id: string;
        name: string;
        initials: string;
        avatarUrl?: string;
        code: string;
        dotsScore: number;
        tier: string;
    }
    interface FriendRequestItem{
        id: string;
        fromAthleteId: string;
        fromName: string;
        fromInitials: string;
        fromAvatarUrl?: string;
        fromCode: string;
        sentAt: string;
    }

    //f1 - friends state
    //todo: replace mock data w/ integration
    const [friendsList, setFriendsList] = useState<FriendItem[]>([]);
    const [requestsLists, setRequestsLists] = useState<FriendRequestItem[]>([]);
    const [userCode, setUserCode] = useState<string>('');
    const [isAddFriendOpen, setIsAddFriendOpen] = useState(false);

    //f2 - profile inspector drawer
    //f3 - arena invites
    //f4 - duel invites

    //handlers
    const fetchFriendsData = async () => {
        await Promise.resolve();
        try {
            const [friendsRes, requestsRes, codeRes] = await Promise.all([
                customFetch('/api/clash/friends'), customFetch('/api/clash/friends/requests'), customFetch('/api/clash/friends/code'),
            ]);
            if (friendsRes.ok){
                const data = await friendsRes.json();
                setFriendsList(data);
            }
            if (requestsRes.ok){
                const data = await requestsRes.json();
                setRequestsLists(data.incoming ?? []);
            }
            if (codeRes.ok){
                const data = await codeRes.json();
                setUserCode(data.code ?? '');
            }
        } catch {
            toast.error('Failed to load friends');
        }
    };

    useEffect(() => {
        void fetchFriendsData();
    }, []);

    //copying ur own code to clip
    const handleCopyCode = () =>{
        if (!userCode) return;
        navigator.clipboard.writeText(userCode);
        setCopiedCode(true);
        toast.success('Your friend code is copied to clipboard', userCode);
        setTimeout(() => setCopiedCode(false), 2000);
    };

    //api call post /respond - accept true
    const handleAcceptRequest = async (requestId: string, name:string)=> {
        try {
            const res = await customFetch(`/api/clash/friends/requests/${requestId}/respond`,{
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    accept: true
                }),
            });
            if (res.ok){
                setRequestsLists((prev) => prev.filter((r) => r.id !== requestId));
                toast.success(`You are now friends with ${name}.`, 'Request Accepted');
                fetchFriendsData();
            } else {
                toast.error('Failed to accept friend request');
            }
        } catch {
            toast.error('Failed to accept friend request');
        }        
    };
    //api call post /respond - accept false
    const handleRejectRequest = async (requestId: string)=> {
        try {
            const res = await customFetch(`/api/clash/friends/requests/${requestId}/respond`,{
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    accept: false
                }),
            });
            if (res.ok){
                setRequestsLists((prev) => prev.filter((r) => r.id !== requestId));
                toast.success('Friend request declined');
                fetchFriendsData();
            } else {
                toast.error('Failed to decline friend request');
            }
        } catch {
            toast.error('Failed to decline friend request');
        }
    }
    //post reject/reject-all
    const handleRejectAllRequests= async () =>{
        try {
            const res = await customFetch('/api/clash/friends/requests/reject-all',{
                method: 'POST',
            });
            if (res.ok){
                setRequestsLists([]);
                toast.info('All pending friend requests have been rejected.')
            } else {
                toast.error('Failed to reject requests');
            }
        } catch {
            toast.error('Failed to reject requests');
        }
        
    };

    //f4 - handle accept duel + handle reject duel 
    //f3 + f4 - handle reject all arena + duel invites

    const filteredFriends = friendsList.filter((f) => f.name.toLowerCase().includes(searchQuery.toLowerCase()) 
    || f.code.toLowerCase().includes(searchQuery.toLowerCase()));
    const filteredRequests = requestsLists.filter((f) => f.fromName.toLowerCase().includes(requestSearchQuery.toLowerCase()) 
    || f.fromCode.toLowerCase().includes(requestSearchQuery.toLowerCase()));

    return (
        <div className="min-h-screen bg-background text-foreground pb-20">
            <div className="max-w-6xl mx-auto px-4 pt-8 pb-4">
                <Link to="/clash" className="inline-flex items-center gap-2 text-xs font-bold text-muted-foreground hover:text-brand uppercase tracking-[1px] mb-4 transition font-sans">
                    <ArrowLeft className="w-4 h-4"/>Back to Arena Hub
                </Link>

                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div>
                        <PageTitle title="FRIENDS AND INVITES"/>
                    </div>

                    {/* users code: todo: replace mockdata */}
                    <div className="flex items-center gap-3 bg-surface border border-border p-3 rounded-xl self-start md:self-auto shadow-sm">
                        <div>
                            <span className="text-[10px] uppercase font-bold text-muted-foreground block font-sans tracking-[1px]">
                                Your Private Friend Code
                            </span>
                            <span className="font-mono text-xl md:text-2xl font-bold text-brand tracking-wider block">
                                {userCode || '------'}
                            </span>
                        </div>
                        <Button variant="secondary" size="sm" onClick={handleCopyCode} className="h-8 px-3 text-xs">
                            {copiedCode ? <Check className="w-3.5 h-3.5 text-success mr-1"/> : <Copy className="w-3.5 h-3.5 mr-1"/>}
                            <span>{copiedCode ? 'Copied' : 'Copy'}</span>
                        </Button>
                    </div>
                </div>

                {/* tabs */}
                <div className="mt-6">
                    <ClashTabs activeTab={activeTab} onChange={(tab) => setActiveTab(tab)}
                        tabs={[
                            { id: 'friends', label: 'My Friends', count: friendsList.length, icon: <Users className="w-3.5 h-3.5"/>},
                            { id: 'requests', label: 'Requests', count: requestsLists.length, icon: <UserPlus className="w-3.5 h-3.5"/>},
                            //f3 + f4 - invites tab
                            ]}/>
                </div>
            </div>

            <div className="max-w-6xl mx-auto px-4 py-4 space-y-6">
                {activeTab === 'friends' &&(
                    <div className="space-y-4">
                        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                            <div className="w-full max-w-md">
                                <SearchInput placeholder="Search friends by name or code" value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                className="h-9 text-xs bg-surface border-border font-sans"/>
                        </div>
                        <Button variant="default" size="sm" onClick={() => setIsAddFriendOpen(true)}
                        className="h-9 text-xs flex items-center gap-1.5 shrink-0">
                            <UserPlus className="w-3.5 h-3.5"/>
                            <span>Add Friend By Code</span>
                        </Button>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {filteredFriends.map((friend) => (
                            <Card key={friend.id} className="bg-surface border-border p-5 shadow-sm flex flex-col justify-between">
                                <CardContent className="p-0">
                                    <div className="flex items-start justify-between">
                                        <div className="flex items-center gap-3.5">
                                            <AthleteAvatar initials={friend.initials} name={friend.name} avatarUrl={friend.avatarUrl} size="lg" className="group-hover:border-brand transition"/>
                                            <div>
                                                <div className="flex items-center gap-2">
                                                    <strong className="font-sans font-bold text-foreground text-base">
                                                        {friend.name}
                                                    </strong>
                                                    <span className="font-mono text-xs font-bold text-brand bg-surface-2 border border-border px-2 py-0.5 rounded">
                                                        {friend.code}
                                                    </span>
                                                </div>
                                                <div className="text-xs text-muted-foreground mt-1 font-sans">
                                                    Score: <strong className="font-bold text-brand">{friend.dotsScore} DOTS</strong>
                                                </div>
                                            </div>
                                        </div>
                                        {/* tier badge */}
                                        <div>
                                            <TierBadge tier={friend.tier} size="md" />
                                        </div>
                                    </div>

                                    {/* f2 - inspect profile btn */}
                                    {/* f4 - challenge 1v1 btn */}
                                </CardContent>
                            </Card>
                        ))}
                        </div>
                    </div>
                )}

                {activeTab === 'requests' && (
                    <div className="space-y-4">
                        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                            <div className="w-full max-w-md">
                                <SearchInput placeholder="Search requests by name or code" value={requestSearchQuery} onChange={(e) => setRequestSearchQuery(e.target.value)}
                                    className="h-9 text-xs bg-surface border-border font-sans"/>
                            </div>

                            {requestsLists.length > 0 && (
                                <Button variant="outline" size="sm" onClick={handleRejectAllRequests}
                                className="h-9 text-xs text-destructive border-destructive/30 hover:bg-destructive/10 flex items-center gap-1.5 shrink-0">
                                    <Trash2 className="w-3.5 h-3.5"/>
                                    <span>Reject All Requests</span>
                                </Button>
                            )}
                            </div>

                        {filteredRequests.length === 0 ? (
                            <Card className="bg-surface border-border p-10 text-center text-muted-foreground font-sans text-xs">
                                {requestsLists.length === 0 ? 'No pending incoming friend requests' : 'No friend requests found matching your search'}
                            </Card>
                        ) : (
                    <div className="space-y-3">
                        {filteredRequests.map((req) => (
                            <Card key={req.id} className="bg-surface border-border p-4 shadow.sm">
                                <CardContent className="p-0 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                                    <div className="flex items-center gap-3.5">
                                        <AthleteAvatar initials={req.fromInitials} name={req.fromName} avatarUrl={req.fromAvatarUrl} size="md"/>
                                        <div>
                                            <div className="flex items-center gap-2">
                                                <strong className="font-sans font-bold text-foreground">
                                                    {req.fromName}
                                                </strong>
                                                <span className="font-mono text-xs font-bold text-brand bg-surface-2 border border-border px-1.5 py-0.5 rounded">
                                                    {req.fromCode}
                                                </span>
                                            </div>
                                            <span className="text-xs text-muted-foreground font-sans block mt-0.5">
                                                Sent {req.sentAt}
                                            </span>
                                        </div>
                                    </div>
                                    {/* btns */}
                                    <div className="flex items-center gap-2 self-end sm:self-auto">
                                        <Button variant="secondary" size="sm" onClick={() => handleRejectRequest(req.id)}
                                        className="h-8 flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
                                            <XCircle className="w-3.5 h-3.5"/>
                                            <span>Reject</span>
                                        </Button>
                                        <Button variant="default" size="sm" onClick={() => handleAcceptRequest(req.id, req.fromName)}
                                        className="h-8 flex items-center gap-1 text-xs">
                                            <CheckCircle2 className="w-3.5 h-3.5"/>
                                            <span>Accept Friend</span>
                                        </Button>
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                        </div>
                    )}
                    </div>
                )}

                {/* f3 + f4 - tab 3 arena + duel invites */}
            </div>

            <AddFriendModal isOpen={isAddFriendOpen} onClose={() => setIsAddFriendOpen(false)} onFriendAdded={() => fetchFriendsData()} />
        </div>
    );
}