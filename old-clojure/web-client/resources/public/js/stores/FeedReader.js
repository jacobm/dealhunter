var Configuration = require('../constants/Configuration');

var readBackwardsHelper = function(link, steps, itemCallback, acc) {
    if(steps <= 0 || link === undefined) {
	console.dir(acc);
	_.each(acc.reverse(), function(accFunc) { accFunc(); });
	return;
    }
    $.get(link.href, function(data) {
	if(data._links.prev != undefined) {
	    acc.push(function() { itemCallback(data, link.href); });
	    readBackwardsHelper(data._links.prev, --steps, itemCallback, acc);
	};
    });
};



var FeedReader = {

    // delivers items to itemCallback in order oldest -> newest
    readBackwards: function(link, steps, itemCallback) {
	readBackwardsHelper(link, steps, itemCallback, []);
    },

    readForwards: function(link, steps, itemCallback) {
	if(steps <= 0 || link === undefined) {
	    return;
	}
	$.get(link.href, function(data) {
	    itemCallback(data, link);
	    if(data._links.next != undefined) {
		this.readForwards(data._links.next, --steps, itemCallback);
	    }
	}.bind(this));
    },

    readNewest: function(searchTerm, count, itemCallback) {
	$.get(Configuration.SEARCH_ENDPOINT + "/feed/" + searchTerm, function(data) {
	    if(data._links.newest != undefined) {
		this.readBackwards(data._links.newest, count, itemCallback);
	    }
	}.bind(this));
    }
};

module.exports = FeedReader;
